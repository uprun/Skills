﻿using System;
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
        private NodeModel CreateNode(SkillsContext context) 
        {
            NodeModel result = null;
            
                var mainNode = context.Nodes.Add(new NodeModel 
                            {
                                tags = new List<TagModel>()
                            }
                        );
                context.SaveChanges();
                result = mainNode.Entity;
            
            return result;
        }

        private NodeModel AddTagToNode(SkillsContext context, long nodeId, string tag, string value)
        {
            NodeModel result = null;
            
            
                var nodeTags = context.Nodes.Where(x => x.id == nodeId).Select(x => x.tags).First();
                nodeTags.Add(new TagModel
                            {
                                tag = tag,
                                value = value
                            });
                context.SaveChanges();
                var mainNode = context.Nodes.First( x => x.id == nodeId);
                result = mainNode;
            
            return result;
        }



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

                if(context.VersionsApplied.FirstOrDefault(x => x.VersionApplied == "nodesAddToProcessField_attempt#1") == null)
                {
                    foreach ( var node in context.Nodes.Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "skill") != null))
                    {
                        AddTagToNode(context, node.id, "field:list", "toProcess");
                        
                    }

                    context.VersionsApplied.Add( new VersionModel
                    { 
                        VersionApplied = "nodesAddToProcessField_attempt#1",
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
                skills =  context.Nodes
                    .Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "skill") != null)
                    .Select(x => new SkillDTO 
                    {
                        NodeId = x.id,
                        SkillName = x.tags.FirstOrDefault(t => t.tag == "name").value,
                        ToProcess = x.tags
                            .Where(t => t.tag == "rid:toProcess" && t.value != "")
                            .Select(t => new LinkDTO{ Url = context.Nodes.First(n => n.id.ToString() == t.value).tags.FirstOrDefault(at => at.tag == "url").value } ).ToList()
                    }).ToArray();
            }
            
            return Json( skills );
        }

        [HttpPost]
        public JsonResult AddUrlToProcess(long HostNodeId, string url)
        {
            using(var context = new SkillsContext())
            {
                var urlNode = CreateNode(context);
                AddTagToNode(context, urlNode.id, "type", "url");
                AddTagToNode(context, urlNode.id, "url", url);
                AddTagToNode(context, HostNodeId, "rid:toProcess", urlNode.id.ToString());
            }
            
            return Json(new LinkDTO 
            {
                Url = url
            });
        }
    }
}
