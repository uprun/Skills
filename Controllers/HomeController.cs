using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Skills.Models;

namespace Skills.Controllers
{
    public class HomeController : Controller
    {
        [HttpPost]
        public JsonResult ApplyMigrations()
        {
            using(var context = new SkillsContext())
            {
                
                if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "migrationToNodeRepresentation_attempt#1") == null)
                {
                    foreach ( var skill in context.Skills)
                    {
                        var mainNode = context.Nodes.Add(new NodeModel 
                            {
                                tags = new List<TagModel>()
                            }
                        );
                        context.SaveChanges();
                        mainNode.Entity.tags.Add(new TagModel
                            {
                                tag = "name",
                                value = skill.SkillName
                            }
                        );

                        mainNode.Entity.tags.Add(new TagModel
                            {
                                tag = "type",
                                value = "skill"
                            }
                        );
                        context.SaveChanges();
                        var toProcess = context.Skills.Where(x => x.Id == skill.Id).Select(x => x.ToProcess).FirstOrDefault();
                        foreach(var link in toProcess)
                        {
                            var linkToAdd = context.Nodes.Add(new NodeModel 
                            {
                                tags = new List<TagModel>()
                            });
                            context.SaveChanges();
                            linkToAdd.Entity.tags.Add(new TagModel
                            {
                                tag = "url",
                                value = link.Url

                            });
                            linkToAdd.Entity.tags.Add(new TagModel
                            {
                                tag = "type",
                                value = "url"
                            });
                            mainNode.Entity.tags.Add(new TagModel
                            {
                                tag = "rid:toProcess",
                                value = linkToAdd.Entity.id.ToString()
                            });
                            context.SaveChanges();
                        }
                    }

                    context.VersionsApplied.Add( new VersionModel
                    { 
                        VersionApplied = "migrationToNodeRepresentation_attempt#1",
                        TimeApplied = DateTime.Now
                    });
                    context.SaveChanges();

                }
                
            }
            return Json(null);
        }
        public IActionResult Index()
        {
            var model = new ViewModels.HomeIndexViewModel();

            if(model.SkillsAvailable == null)
            {
                model.SkillsAvailable = new SkillDTO[0];
            }
            return View(model);
        }

        public SkillModel NewSkill(string skillName)
        {
            SkillModel toReturn = null;
            using(var context = new SkillsContext())
            {
                var result = context.Skills.FirstOrDefault(x => x.SkillName == skillName);
                if(result == null)
                {
                    toReturn = new SkillModel
                    {
                        SkillName = skillName,
                        MinutesSpent = 0

                    };
                    var result2 = context.Skills.Add(toReturn);
                    context.SaveChanges();
                    toReturn =  result2.Entity;

                }
            }
            return toReturn;
        }

        [HttpPost]
        public JsonResult GetSkillsAvailable()
        {
            SkillDTO[] skills = null;

            using(var context = new SkillsContext())
            {
                skills =  context.Skills.Select(x => new SkillDTO 
                {
                    SkillName = x.SkillName,
                    MinutesSpent = x.MinutesSpent,
                    ToProcess = x.ToProcess.Select(y => new LinkDTO(y)).ToList()
                }).ToArray();
            }
            
            return Json( skills );
        }

        [HttpPost]
        public JsonResult AddUrlToProcess(string skill, string url)
        {
            /// this can be auto-generated like "add if not exists"
            ///
            using(var context = new SkillsContext())
            {
                var toProcessOfFoundSkill = context.Skills.Where(x => x.SkillName == skill ).Select(x => x.ToProcess).FirstOrDefault();
                if(toProcessOfFoundSkill != null)
                {                     
                    if(!toProcessOfFoundSkill.Exists(x => x.Url == url))
                    {
                        var urlAdded = new LinkModel
                        {
                            Url = url

                        };
                         toProcessOfFoundSkill.Add(urlAdded);
                         context.SaveChanges();
                         return Json(new LinkDTO(urlAdded));

                    }
                        
                }
            }
            return null;
        }

        

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
