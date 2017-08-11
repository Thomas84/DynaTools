﻿//
// Copyright 2015 Autodesk, Inc.
// Author: Thornton Tomasetti Ltd, CORE Studio (Maximilian Thumfart)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using DSRevitNodesUI;
using RVT = Autodesk.Revit.DB;
using RevitServices.Persistence;
using RevitServices.Transactions;

using Dynamo.Utilities;
using Dynamo.Models;
using ProtoCore.AST.AssociativeAST;

namespace DropDown
{

    public abstract class CustomRevitElementDropDown : RevitDropDownBase
    {
        /// <summary>
        /// Generic Revit Element Class Dropdown Node
        /// </summary>
        /// <param name="name">Name of the Node</param>
        /// <param name="elementType">Type of Revit Element to display</param>
        public CustomRevitElementDropDown(string name, Type elementType) : base(name) { this.ElementType = elementType; PopulateItems(); }

        /// <summary>
        /// Constant message for missing Types
        /// </summary>
        private const string noTypes = "No Types available.";

        /// <summary>
        /// Type of Element
        /// </summary>
        public Type ElementType;

        protected override CoreNodeModels.DSDropDownBase.SelectionState PopulateItemsCore(string currentSelection)
        {
            PopulateItems();
            return SelectionState.Done;
        }

        /// <summary>
        /// Populate the Dropdown menu
        /// </summary>
        public void PopulateItems()
        {
            if (this.ElementType != null)
            {
                // Clear the Items
                Items.Clear();

                // Set up a new element collector using the Type field
                var fec = new RVT.FilteredElementCollector(DocumentManager.Instance.CurrentDBDocument).OfClass(ElementType);

                // If there is nothing in the collector add the missing Type message to the Dropdown menu.
                if (fec.ToElements().Count == 0)
                {
                    Items.Add(new CoreNodeModels.DynamoDropDownItem(noTypes, null));
                    SelectedIndex = 0;
                    return;
                }

                if (this.ElementType.FullName == "Autodesk.Revit.DB.Structure.RebarHookType") Items.Add(new CoreNodeModels.DynamoDropDownItem("None", null));

                // Walk through all elements in the collector and add them to the dropdown
                foreach (var ft in fec.ToElements())
                {
                    Items.Add(new CoreNodeModels.DynamoDropDownItem(ft.Name, ft));
                }

                Items = Items.OrderBy(x => x.Name).ToObservableCollection();
            }
        }

        /// <summary>
        /// Cast the selected element to a dynamo node
        /// </summary>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // If there are no elements in the dropdown or the selected Index is invalid return a Null node.
            if (Items.Count == 0 ||
            Items[0].Name == noTypes ||
            SelectedIndex == -1 || Items[SelectedIndex].Name == "None")
            {
                return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), AstFactory.BuildNullNode()) };
            }

            // Assume we are going to cast a Revit Element
            Type genericElementType = typeof(Autodesk.Revit.DB.Element);

            // Cast the selected object to a Revit Element and get its Id
            Autodesk.Revit.DB.ElementId Id = ((Autodesk.Revit.DB.Element)Items[SelectedIndex].Item).Id;

            // Select the element using the elementIds Integer Value
            var node = AstFactory.BuildFunctionCall("Revit.Elements.ElementSelector", "ByElementId",
                new List<AssociativeNode> { AstFactory.BuildIntNode(Id.IntegerValue) });

            // Return the selected element
            return new[] { AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), node) };
        }
    }


    public abstract class CustomGenericEnumerationDropDown : RevitDropDownBase
    {
        /// <summary>
        /// Generic Enumeration Dropdown
        /// </summary>
        /// <param name="name">Node Name</param>
        /// <param name="enumerationType">Type of Enumeration to Display</param>
        public CustomGenericEnumerationDropDown(string name, Type enumerationType) : base(name) { this.EnumerationType = enumerationType; PopulateItems(); }

        /// <summary>
        /// Type of Enumeration
        /// </summary>
        public Type EnumerationType;

        protected override CoreNodeModels.DSDropDownBase.SelectionState PopulateItemsCore(string currentSelection)
        {
            PopulateItems();
            return SelectionState.Done;
        }

        /// <summary>
        /// Populate Items in Dropdown menu
        /// </summary>
        public void PopulateItems()
        {
            if (this.EnumerationType != null)
            {
                // Clear the dropdown list
                Items.Clear();

                // Get all enumeration names and add them to the dropdown menu
                foreach (string name in Enum.GetNames(EnumerationType))
                {
                    Items.Add(new CoreNodeModels.DynamoDropDownItem(name, Enum.Parse(EnumerationType, name)));
                }

                Items = Items.OrderBy(x => x.Name).ToObservableCollection();
            }
        }

        /// <summary>
        /// Assign the selected Enumeration value to the output
        /// </summary>
        public override IEnumerable<AssociativeNode> BuildOutputAst(List<AssociativeNode> inputAstNodes)
        {
            // If there the dropdown is still empty try to populate it again
            if (Items.Count == 0 || Items.Count == -1)
            {
                PopulateItems();
            }

            // get the selected items name
            var stringNode = AstFactory.BuildStringNode((string)Items[SelectedIndex].Name);

            // assign the selected name to an actual enumeration value
            var assign = AstFactory.BuildAssignment(GetAstIdentifierForOutputIndex(0), stringNode);

            // return the enumeration value
            return new List<AssociativeNode> { assign };
        }
    }
}