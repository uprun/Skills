using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Skills.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.IO;

namespace Skills.Controllers
{
    public class HomeController : Controller
    {
        

        [HttpPost]
        public JsonResult MakeNewNode()
        {
            return Json(new toolkit.NodeToolkit().MakeNewNode());
        }

        [HttpPost]
        public JsonResult AddTag(NodeModel model, string tag, string value)
        {
            return Json(new toolkit.NodeToolkit().AddTag(model, tag, value));
        }

        [HttpPost]
        public JsonResult ApplyChanges(NodeModel model)
        {
            return Json(new toolkit.NodeToolkit().ApplyChanges(model));
        }

        [HttpPost]
        public JsonResult ApplyMigrations()
        {
            new nodemigration.migration().migrate(new toolkit.NodeToolkit());
            return Json(null);
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetSkillsAvailable()
        {
            NodeModel[] skills = null;

            using(var context = new SkillsContext())
            {
                
                var allNodes = context.Nodes
                    .Include(n => n.tags)
                    .ToList();
                
                var nodesLookup = allNodes.ToDictionary(n => n.id);
                allNodes.ForEach(n => {
                    var previousTag = n.tags.FirstOrDefault(x => x.tag == "system-reference:previous");
                    if(previousTag != null) 
                    {
                        long previousId;
                        bool parsable = long.TryParse(previousTag.value, out previousId);
                        if(parsable && nodesLookup.ContainsKey(previousId))
                        {
                            nodesLookup[previousId] = null;
                        }
                    }
                });

                var skillTypes = allNodes
                    .Where(x => x.tags.Exists(t => t.tag == "type" && t.value == "instanceTemplate"))
                    .Where(x => x.tags.Exists(t => t.tag == "%type" && t.value == "skill"))
                    .ToDictionary(x => x.id);

                skills = nodesLookup.Values.Where(n => {
                    if(n == null)
                    {
                        return false;
                    }
                    else
                    {
                        var typeTag = n.tags.FirstOrDefault(x => x.tag == "system-reference:type");
                        if(typeTag != null) 
                        {
                            long typeId;
                            bool parsable = long.TryParse(typeTag.value, out typeId);
                            if(parsable && skillTypes.ContainsKey(typeId))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }

                    }
                }).ToArray();
                
            }
            
            return Json( skills );
        }

        [HttpPost]
        public JsonResult GetTemplatesAvailable()
        {
            NodeModel[] skills = null;

            using(var context = new SkillsContext())
            {
                skills = context.Nodes
                    .Include(n => n.tags)
                    .Where(n => n.tags.FirstOrDefault(t => t.tag == "type" && t.value == "instanceTemplate") != null)
                    .ToArray();
            }
            
            return Json( skills );
        }

        [HttpPost]
        public JsonResult CreateNodeFromTemplate(int nodeId)
        {
            
                return Json(new toolkit.NodeToolkit().CreateNodeFromTemplate(nodeId));
        }
    }
}
