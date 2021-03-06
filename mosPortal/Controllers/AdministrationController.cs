﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mosPortal.Data;
using mosPortal.Models;
using mosPortal.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mosPortal.Controllers
{
    public class AdministrationController : Controller
    {
        private dbbuergerContext db = new dbbuergerContext();
        private SignInManager<User> signInManager;
        private UserManager<User> userManager;

        public AdministrationController(UserManager<User> userManager, SignInManager<User> signManager)
        {
            this.userManager = userManager;
            this.signInManager = signManager;
        }
        public IActionResult Index()
        {
            ViewData["ConcernStatusOneCount"] = db.Concern.Where(c=> c.StatusId == 1).Count();
            ViewData["ConcernStatusTwoCount"] = db.Concern.Where(c => c.StatusId == 2).Include("UserConcern").Where(c=> c.UserConcern.Count >=1).Count();
            ViewData["ConcernStatusThreeCount"] = db.Concern.Where(c => c.StatusId == 3).Count();
            //Ablauf Datum!!!
            ViewData["PollCount"] = db.Poll.Count();
            ViewData["CurrentPolls"] = db.Poll.Where(p => p.End >= DateTime.UtcNow).OrderBy(p=> p.End).Take(4).ToList();
            return View("Index");
        }
        public async Task<IActionResult> ShowConcerns(int statusId = 0)
        {
            List<SelectListItem> statusList = new List<SelectListItem>();
            var user = await userManager.GetUserAsync(HttpContext.User);
            IList<string> roles = await userManager.GetRolesAsync(user);
            List<Concern> concerns = new List<Concern>();
            List<Status> status = new List<Status>();
            statusList.Add(new SelectListItem { Value = "0", Text = "Alle Anliegen" });
            if (roles[0]== "Verwaltung")
            {
                concerns = db.Concern.Where(c => c.StatusId != 4 && c.StatusId != 5).ToList();
                status = db.Status.Where(s => s.Id != 4 && s.Id != 5).ToList();
            }
            if(roles[0] == "Gemeinderat")
            {
                concerns = db.Concern.Where(c => c.StatusId != 1 && c.StatusId != 5).ToList();
                status = db.Status.Where(s => s.Id != 1 && s.Id != 5).ToList();
            }
            if(roles[0] == "Admin")
            {
                concerns = db.Concern.ToList();
                status = db.Status.ToList();
            }
            foreach (Concern concern in concerns)
            {
                concern.Comment = db.Comment.Where(c => c.ConcernId == concern.Id).ToList();
                concern.UserConcern = db.UserConcern.Where(uc => uc.ConcernId == concern.Id).ToList();
            }
            foreach (Status stat in status)
            {
                statusList.Add(new SelectListItem { Value = stat.Id.ToString(), Text = stat.Description });
            }
            ViewData["statusList"] = statusList;
            List<SelectListItem> categoriesList = new List<SelectListItem>();
            List<Category> categories = db.Category.ToList();
            foreach (Category category in categories)
            {
                categoriesList.Add(new SelectListItem { Value = category.Id.ToString(), Text = category.Description });
            }
            ViewData["CategoriesList"] = categoriesList;
            return View("ConcernsAdministrationView", concerns);
            /*if (statusId == 0)
            {
                concerns = db.Concern.ToList();
                foreach (Concern concern in concerns)
                {
                    concern.Comment = db.Comment.Where(c => c.ConcernId == concern.Id).ToList();
                    concern.UserConcern = db.UserConcern.Where(uc => uc.ConcernId == concern.Id).ToList();
                }
                return View("ConcernsAdministrationView", concerns);
            }
            
            //ViewData["Status"] = db.Status.Where(s => s.Id == statusId).SingleOrDefault().Description;
            if (statusId != 2)
            {
                concerns = db.Concern
                    .Where(c => c.StatusId == statusId)
                    .Include("Category")
                    .Where(c => c.CategoryId == c.Category.Id)
                    .Include("Status")
                    .Where(c => c.StatusId == c.Status.Id)
                    .Select(x => new Concern
                    {
                        Id = x.Id,
                        Text = x.Text,
                        Title = x.Title,
                        Date = x.Date,
                        StatusId = x.StatusId,
                        Category = x.Category,
                        UserId = x.UserId,
                        Status = x.Status
                    }).ToList();
                foreach (Concern concern in concerns)
                {
                    List<UserConcern> userConcerns = db.UserConcern.Where(uc => uc.ConcernId == concern.Id).ToList();
                    concern.UserConcern = userConcerns;
                }
                return View("ConcernsAdministrationView", concerns);
            }
            else
            {
                concerns = db.Concern.Where(c => c.StatusId == statusId).Include("UserConcern").Where(c => c.UserConcern.Count >= 1).ToList();
                return View("ConcernsAdministrationView", concerns);
            }*/
        }
        [HttpGet]
        public IActionResult CreatePoll()
        {
            Poll poll = new Poll();
            List<SelectListItem> categoriesList = new List<SelectListItem>();
            List<SelectListItem> answerOptionList = new List<SelectListItem>();
            List<Category> categories = db.Category.ToList();
            List<AnswerOptions> answerOptions = db.AnswerOptions.ToList();
            foreach (Category category in categories)
            {
                categoriesList.Add(new SelectListItem { Value = category.Id.ToString(), Text = category.Description });
            }
            foreach(AnswerOptions anserOption in answerOptions)
            {
                answerOptionList.Add(new SelectListItem { Value = anserOption.Id.ToString(), Text = anserOption.Description });
            }
            ViewData["CategoriesList"] = categoriesList;
            ViewData["AnswerOptionsList"] = answerOptionList;
            return View("CreatePollAdministrationView", poll);
        }
        [HttpPost]
        public async Task<IActionResult> CreatePollAsync([FromBody]CreatePollModel createPoll)
        {

            if(ModelState.IsValid)
            {
                
                Concern concern = db.Concern.Where(c => c.Id == createPoll.ConcernId).SingleOrDefault();
                int concernStatusId = concern.StatusId;
                concern.StatusId = 5;
                db.Update(concern);
                int userId = (await userManager.GetUserAsync(HttpContext.User)).Id;
                List<int> answerOptionId = new List<int>();
                Poll poll = new Poll
                {
                    Text = createPoll.Text,
                    Title = createPoll.Title,
                    End = createPoll.End,
                    NeedsLocalCouncil = createPoll.NeedsLocalCouncil,
                    LastUpdatedBy = userId,
                    LastUpdatedAt = DateTime.UtcNow,
                    UserId = userId,
                    StatusId = 2,
                    CategoryId = createPoll.CategoryId
                };
                foreach (string answer in createPoll.Answers)
                {
                    AnswerOptions answerOption = null;
                    answerOption = db.AnswerOptions.Where(ao => ao.Description.Equals(answer)).SingleOrDefault();
                    if(answerOption != null)
                    {
                        answerOptionId.Add(answerOption.Id);
                    }
                    else
                    {
                        answerOption = new AnswerOptions { Description = answer };
                        db.Add(answerOption);
                        //db.SaveChanges();
                        answerOptionId.Add(answerOption.Id);
                    }
                }
                if(!poll.NeedsLocalCouncil)
                {
                    poll.Approved = true;
                }
                else
                {
                    poll.Approved = false;
                }
                db.Add(poll);
                //db.SaveChanges();
                foreach(int aoId in answerOptionId)
                {
                    db.Add(new AnswerOptionsPoll { PollId = poll.Id, AnswerOptionsId = aoId });
                    //db.SaveChanges();
                }
                int result = db.SaveChanges();
                return Json(new { result , concernStatusId, concernId = createPoll.ConcernId});
            }
            else
            {
                return Json(new { result = 0 });
            }
            
        }

        public IActionResult ShowConcernsLocalCouncil()
        {
            List<Concern> concerns = db.Concern.Where(c => c.StatusId >= 2 && c.StatusId <= 3).Include("UserConcern").Where(c => c.UserConcern.Count >= 1).ToList();
            return View("ConcernsAdministrationView", concerns);
        }
        public JsonResult GetConcernJson(int concernId)
        {
            Concern concern = db.Concern.Where(c => c.Id == concernId).SingleOrDefault();
            Status[] statuses = db.Status.Where(s=> s.Id >= concern.StatusId).ToArray();
            string statusesJson = Newtonsoft.Json.JsonConvert.SerializeObject(statuses);
            int categoryId = concern.CategoryId;
            return Json(new { concernId, title = concern.Title, text= concern.Text,categoryId,statusId = concern.StatusId ,date = concern.Date.ToString(), statuses, });
        }
        /*[HttpPost]
        public async Task<IActionResult> ChangeConcernStatus(string concernModalStatus, string concernModalId)
        {
            int concernId = Convert.ToInt32(concernModalId);
            int statusId = Convert.ToInt32(concernModalStatus);
            Concern concern = db.Concern.Where(c => c.Id == concernId).SingleOrDefault();
            int oldStatus = concern.StatusId;
            concern.StatusId = statusId;
            db.Concern.Update(concern);
            db.SaveChanges();
            return await this.ShowConcerns(oldStatus);

        }*/
        [HttpPost]
        public JsonResult ChangeConcernStatus(string status, string concern)
        {
            int concernId = Convert.ToInt32(concern);
            int statusId = Convert.ToInt32(status);
            Concern con = db.Concern.Where(c => c.Id == concernId).SingleOrDefault();
            int oldStatus = con.StatusId;
            con.StatusId = statusId;
            db.Concern.Update(con);
            int result = db.SaveChanges();
            return Json(new { result });

        }
        public IActionResult ShowPolls()
        {
            List<Poll> polls = db.Poll.ToList();
            List<SelectListItem> categoriesList = new List<SelectListItem>();
            List<SelectListItem> answerOptionList = new List<SelectListItem>();
            List<Category> categories = db.Category.ToList();
            List<AnswerOptions> answerOptions = db.AnswerOptions.ToList();
            foreach (Category category in categories)
            {
                categoriesList.Add(new SelectListItem { Value = category.Id.ToString(), Text = category.Description });
            }
            foreach (AnswerOptions anserOption in answerOptions)
            {
                answerOptionList.Add(new SelectListItem { Value = anserOption.Id.ToString(), Text = anserOption.Description });
            }
            ViewData["CategoriesList"] = categoriesList;
            ViewData["AnswerOptionsList"] = answerOptionList;
            /*foreach(Poll poll in polls)
            {
                List<AnswerOptionsPoll> answerOptionsPolls = db.AnswerOptionsPoll.Where(aop => aop.PollId == poll.Id).Include("AnswerOptions").Where(aop => aop.AnswerOptionsId == aop.AnswerOptions.Id).ToList();
                poll.AnswerOptionsPoll = answerOptionsPolls;
            }*/

            return View("PollsAdministrationView", polls );
        }
        public IActionResult ShowPoll()
        {
            int id = 2;
            Poll poll = db.Poll.Where(p => p.Id == id).SingleOrDefault();
            List<AnswerOptionsPoll> answerOptionsPolls = db.AnswerOptionsPoll.Where(aop => aop.PollId == poll.Id).Include("AnswerOptions").Where(aop => aop.AnswerOptionsId == aop.AnswerOptions.Id).ToList();
            poll.AnswerOptionsPoll = answerOptionsPolls;
            return View("PollAdministrationView", poll);
        }
        public JsonResult GetPollAnswers(int pollId)
        {
            int id = pollId;
            Poll poll = db.Poll.Where(p => p.Id == id).SingleOrDefault();
            List<AnswerOptionsPoll> answerOptionsPolls = db.AnswerOptionsPoll.Where(aop => aop.PollId == poll.Id).Include("AnswerOptions").Where(aop => aop.AnswerOptionsId == aop.AnswerOptions.Id).ToList();
            foreach (AnswerOptionsPoll answerOptionsPoll in answerOptionsPolls)
            {
                List<UserAnswerOptionsPoll> userAnswerOptionsPolls = db.UserAnswerOptionsPoll.Where(uaop => uaop.AnswerOptionsPollId == answerOptionsPoll.Id).ToList();
                answerOptionsPoll.UserAnswerOptionsPoll = userAnswerOptionsPolls;
            }
            poll.AnswerOptionsPoll = answerOptionsPolls;

            return poll.getAnswers();

        }
        }
}
