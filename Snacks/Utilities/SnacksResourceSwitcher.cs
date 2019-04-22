﻿/**
The MIT License (MIT)
Copyright (c) 2014-2019 by Michael Billard
Original concept by Troy Gruetzmacher

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;


namespace Snacks
{
    public struct ResourceOption
    {
        public string name;
        public ConfigNode[] resourceConfigs;
    }

    public class SnacksResourceSwitcher : BaseSwitcher
    {
        protected ResourceOption[] resourceOptions = null;

        public override void ToggleOption()
        {
            base.ToggleOption();

            LoadOptionResources(true);
        }

        public override string GetInfo()
        {
            if (resourceOptions == null)
            {
                GetOptionNodes();
                checkDefaultOption();
                if (resourceOptions == null)
                    return "Supports multiple resource configurations";
            }

            StringBuilder info = new StringBuilder();

            info.AppendLine("<b>Resource Options</b>");
            info.AppendLine(" ");

            ResourceOption option;
            ConfigNode node;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            for (int index = 0; index < resourceOptions.Length; index++)
            {
                option = resourceOptions[index];

                info.AppendLine("<color=orange><b>" + option.name + "</b></color>");

                for (int resourceIndex =  0; resourceIndex < option.resourceConfigs.Length; resourceIndex++)
                {
                    node = option.resourceConfigs[resourceIndex];
                    if (node.HasValue("name") && node.HasValue("amount") && node.HasValue("maxAmount"))
                    {
                        if (definitions.Contains(node.GetValue("name")))
                        {
                            resourceDef = definitions[node.GetValue("name")];
                            info.AppendLine("\t- " + resourceDef.displayName + ": " + node.GetValue("amount") + "/" + node.GetValue("maxAmount"));
                        }
                    }
                }
                info.AppendLine(" ");
            }

            return info.ToString();
        }

        public override void OnAwake()
        {
            base.OnAwake();
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            //If we don't have any resources then make sure to load the defaults.
            if (this.part.Resources.Count == 0)
                LoadOptionResources();
        }

        public override ConfigNode[] GetOptionNodes(string nodeName = kOptionNode)
        {
            ConfigNode[] nodes =  base.GetOptionNodes(nodeName);
            if (nodes == null)
                return null;
            ConfigNode node;
            ResourceOption resourceOption;
            List<ResourceOption> resourceOptionList = new List<ResourceOption>();

            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];

                if (node.HasNode("RESOURCE") && node.HasValue("name"))
                {
                    resourceOption = new ResourceOption();
                    resourceOption.name = node.GetValue("name");
                    resourceOption.resourceConfigs = node.GetNodes("RESOURCE");
                    resourceOptionList.Add(resourceOption);
                }
            }
            if (resourceOptionList.Count > 0)
                resourceOptions = resourceOptionList.ToArray();

            return nodes;
        }

        public virtual void RemoveAllResources()
        {
            List<PartResource> doomedResources = new List<PartResource>();
            PartResource[] resources = this.part.Resources.ToArray<PartResource>();

            for (int index = 0; index < resources.Length; index++)
                doomedResources.Add(resources[index]);

            resources = doomedResources.ToArray();
            for (int index = 0; index < resources.Length; index++)
                RemoveResource(resources[index].resourceName);

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
        }

        public virtual void RemoveResource(string resourceName)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            int resourceID = definitions[resourceName].id;

            this.part.Resources.dict.Remove(resourceID);
        }

        protected void LoadOptionResources(bool updateSymmetryParts = false)
        {
            ConfigNode[] resourceConfigs;
            Part[] symmetryParts;
            SnacksResourceSwitcher switcher;

            //Clear our resources
            RemoveAllResources();

            //Set the option name
            Events["ToggleOption"].guiName = resourceOptions[currentOptionIndex].name;

            //Get the resource configs
            resourceConfigs = resourceOptions[currentOptionIndex].resourceConfigs;

            //Now add the resources to the part.
            for (int index = 0; index < resourceConfigs.Length; index++)
                this.part.AddResource(resourceConfigs[index]);

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);

            //Update symmetry parts
            if (updateSymmetryParts && this.part.symmetryCounterparts.Count > 0)
            {
                symmetryParts = this.part.symmetryCounterparts.ToArray();
                for (int index = 0; index < symmetryParts.Length; index++)
                {
                    switcher = symmetryParts[index].FindModuleImplementing<SnacksResourceSwitcher>();
                    switcher.currentOptionIndex = this.currentOptionIndex;
                    switcher.LoadOptionResources();
                }
            }
        }
    }
}
