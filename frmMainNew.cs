﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace AIEdit
{
	using IniDictionary = Dictionary<string, OrderedDictionary>;

	public partial class frmMainNew : Form
	{
		public frmMainNew()
		{
			InitializeComponent();
		}

		public TaskForce SelectedTaskForce()
		{
			return olvTF.SelectedObject as TaskForce;
		}

		public ScriptType SelectedScriptType()
		{
			return olvST.SelectedObject as ScriptType;
		}

		private void mnuLoadRA_Click(object sender, EventArgs e)
		{
			IniDictionary config = IniParser.ParseToDictionary("config/ra2.ini");
			OrderedDictionary general = config["General"];
			string sectionHouses = general["Houses"] as string;
			string editorName = general["EditorName"] as string;



			LoadRules("rulesmd.ini", sectionHouses, editorName);
			LoadConfig(config);
			LoadAI("aimd.ini");

			// cmb tf unit
			cmbTFUnit.Items.Clear();
			foreach(TechnoType entry in unitTypes) cmbTFUnit.Items.Add(entry);
			cmbTFUnit.SelectedIndex = 0;

			// cmb tf group
			cmbTFGroup.Items.Clear();
			foreach (GroupType gt in groupTypes) cmbTFGroup.Items.Add(gt);
			cmbTFGroup.SelectedIndex = 0;
			
			olvTF.Sort(olvColTFName, SortOrder.Ascending);
			olvTF.SetObjects(taskForces.Items);

			olvST.Sort(olvColSTName, SortOrder.Ascending);
			olvST.SetObjects(scriptTypes.Items);
		}

		private void UpdateTFUnit(int mod)
		{
			TechnoType tt = cmbTFUnit.SelectedItem as TechnoType;
			TaskForce tf = SelectedTaskForce();

			if (tf.Mod(tt, mod) != 0)
			{
				olvTFUnits.SetObjects(tf);
			}
			else
			{
				TaskForceEntry tfe = tf.SingleOrDefault(s => s.Unit == tt);
				olvTFUnits.RefreshObject(tfe);
			}

			UpdateTFCost();
		}

		private void btnTFAddUnit_Click(object sender, EventArgs e)
		{
			UpdateTFUnit(1);
		}

		private void btnTFDelUnit_Click(object sender, EventArgs e)
		{
			UpdateTFUnit(-1);
		}

		private void saveAIToolStripMenuItem_Click(object sender, EventArgs e)
		{
			WriteAI("aimd_out.ini");
		}

		private void DelActiveTF()
		{
			DialogResult res = MessageBox.Show("Are you sure you want to delete this Task Force?",
				"Delete Task Force", MessageBoxButtons.YesNo);

			if (res == DialogResult.Yes)
			{
				int idx = Math.Min(olvTF.SelectedIndex, olvTF.Items.Count - 1);
				TaskForce tf = SelectedTaskForce();
				taskForces.Remove(tf);
				olvTF.BeginUpdate();
				olvTF.RemoveObject(tf);
				olvTF.EndUpdate();
				olvTF.SelectedIndex = idx;
			}
		}

		private void DelActiveST()
		{
			DialogResult res = MessageBox.Show("Are you sure you want to delete this Script Type?",
				"Delete Script Type", MessageBoxButtons.YesNo);

			if (res == DialogResult.Yes)
			{
				int idx = Math.Min(olvST.SelectedIndex, olvST.Items.Count - 1);
				ScriptType st = SelectedScriptType();
				scriptTypes.Remove(st);
				olvST.BeginUpdate();
				olvST.RemoveObject(st);
				olvST.EndUpdate();
				olvST.SelectedIndex = idx;
			}
		}


		private void STActionMoveUp()
		{
			ScriptType st = SelectedScriptType();
			int idx = st.MoveUp(olvSTActions.SelectedIndex);
			olvSTActions.SetObjects(st.Actions);
			olvSTActions.SelectedIndex = idx;
		}

		private void STActionMoveDown()
		{
			ScriptType st = SelectedScriptType();
			ScriptAction a = olvSTActions.SelectedObject as ScriptAction;
			int idx = st.MoveDown(olvSTActions.SelectedIndex);
			olvSTActions.SetObjects(st.Actions);
			olvSTActions.SelectedIndex = idx;
		}

		private void STActionNew()
		{
			ScriptAction sa = new ScriptAction(actionTypes[0], 0);
			ScriptType st = SelectedScriptType();
			int idx = olvSTActions.SelectedIndex + 1;
			st.Insert(sa, idx);
			olvSTActions.SetObjects(st.Actions);
			olvSTActions.SelectedIndex = idx;
		}

		private void STActionDelete()
		{
			ScriptAction sa = olvSTActions.SelectedObject as ScriptAction;
			ScriptType st = SelectedScriptType();
			int idx = olvSTActions.SelectedIndex;
			st.Remove(sa);
			olvSTActions.SetObjects(st.Actions);
			idx = Math.Min(idx, st.Count - 1);
			olvSTActions.SelectedIndex = idx;
		}


		/**
		 * Control Delegates.
		 **/


		private void mnuDelTF_Click(object sender, EventArgs e)
		{
			DelActiveTF();
		}

		private void mnuNewTF_Click(object sender, EventArgs e)
		{
			InputBox.InputResult res = InputBox.Show("New Task Force", "Enter name:");

			if (res.ReturnCode == DialogResult.OK)
			{
				string id = nextID();
				TaskForce tf = new TaskForce(id, res.Text, groupTypes[0]);
				taskForces.Add(tf);
				olvTF.BeginUpdate();
				olvTF.AddObject(tf);
				olvTF.EndUpdate();
				olvTF.SelectedObject = tf;
			}
		}

		private void olvTF_SelectedIndexChanged(object sender, EventArgs e)
		{
			TaskForce tf = SelectedTaskForce();
			if (tf == null) return;
			olvTFUnits.SetObjects(tf);
			olvTFUnits.SelectedIndex = 0;
			cmbTFGroup.SelectedItem = tf.Group;
			UpdateTFCost();
		}

		private void olvTF_CellEditFinished(object sender, BrightIdeasSoftware.CellEditEventArgs e)
		{
			olvTF.Sort();
		}

		private void olvTFUnits_CellEditStarting(object sender, BrightIdeasSoftware.CellEditEventArgs e)
		{
			if (e.SubItemIndex == 1)
			{
				TaskForceEntry tfe = e.RowObject as TaskForceEntry;
				int idx = unitTypes.TakeWhile(s => s != tfe.Unit).Count();

				// Unit selector
				ComboBox cmb = new ComboBox();
				cmb.FlatStyle = FlatStyle.Flat;
				cmb.DropDownStyle = ComboBoxStyle.DropDownList;
				foreach (TechnoType entry in unitTypes) cmb.Items.Add(entry);
				cmb.Bounds = e.CellBounds;
				cmb.SelectedIndex = idx;
				e.Control = cmb;
			}
		}

		private void olvTFUnits_CellEditFinishing(object sender, BrightIdeasSoftware.CellEditEventArgs e)
		{
			if(!e.Cancel && e.SubItemIndex == 1)
			{
				ComboBox cmb = e.Control as ComboBox;
				TaskForce tf = SelectedTaskForce();
				TaskForceEntry tfe = e.RowObject as TaskForceEntry;
				TechnoType unit = cmb.SelectedItem as TechnoType;

				TaskForceEntry exists = tf.SingleOrDefault(s => s.Unit == unit);

				if (exists != null && exists != tfe)
				{
					tf.Remove(tfe.Unit);
					exists.Count = exists.Count + tfe.Count;
					olvTFUnits.SetObjects(tf);
				}
				else
				{
					tfe.Unit = unit;
					olvTFUnits.RefreshItem(e.ListViewItem);
				}
			}
		}

		private void olvTFUnits_CellEditFinished(object sender, BrightIdeasSoftware.CellEditEventArgs e)
		{
			TaskForce tf = SelectedTaskForce();
			if (e.SubItemIndex == 0)
			{
				uint val = (uint)e.NewValue;
				TaskForceEntry tfe = e.RowObject as TaskForceEntry;

				if (val == 0)
				{
					tf.Remove(tfe.Unit);
					olvTFUnits.SetObjects(tf);
				}
			}

			UpdateTFCost();
		}

		private void olvTF_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete) DelActiveTF();
		}

		private void olvTFUnits_KeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Delete)
			{
				TaskForce tf = SelectedTaskForce();
				TaskForceEntry tfe = olvTFUnits.SelectedObject as TaskForceEntry;
				tf.Remove(tfe.Unit);
				olvTFUnits.SetObjects(tf);
			}
		}

		private void cmbTFGroup_SelectionChangeCommitted(object sender, EventArgs e)
		{
			TaskForce tf = SelectedTaskForce();
			tf.Group = cmbTFGroup.SelectedItem as GroupType;
		}

		private void olvST_SelectedIndexChanged(object sender, EventArgs e)
		{
			ScriptType st = SelectedScriptType();
			if (st == null) return;
			olvSTActions.SetObjects(st);
			olvSTActions.SelectedIndex = 0;
		}

		private void olvSTActions_FormatRow(object sender, BrightIdeasSoftware.FormatRowEventArgs e)
		{
			// numbers every row
			e.Item.SubItems[0].Text = e.DisplayIndex.ToString();
		}

		private void olvST_CellEditFinished(object sender, BrightIdeasSoftware.CellEditEventArgs e)
		{
			olvST.Sort();
		}


		private void UpdateSTActionDescription()
		{
			ScriptAction action = olvSTActions.SelectedObject as ScriptAction;
			if (action != null) txtSTActionDesc.Text = action.Action.Description;
		}

		private void olvSTActions_CellEditStarting(object sender, BrightIdeasSoftware.CellEditEventArgs e)
		{
			ScriptAction action = e.RowObject as ScriptAction;

			// action
			if (e.SubItemIndex == 1)
			{
				uint idx = action.Action.Code;

				ComboBox cmb = new ComboBox();
				cmb.FlatStyle = FlatStyle.Flat;
				cmb.DropDownStyle = ComboBoxStyle.DropDownList;
				cmb.Sorted = true;
				foreach (IActionType entry in actionTypes) cmb.Items.Add(entry);
				cmb.SelectedItem = action.Action;
				cmb.Bounds = e.CellBounds;
				e.Control = cmb;
			}
			// parameter
			else if(e.SubItemIndex == 2)
			{
				if(action.Action.ParamType == ScriptParamType.Number)
				{
					NumericUpDown nud = new NumericUpDown();
					nud.Minimum = 0;
					nud.Value = action.Param;
					nud.Bounds = e.CellBounds;
					e.Control = nud;
				}
				else
				{
					ComboBox cmb = new ComboBox();
					cmb.FlatStyle = FlatStyle.Flat;
					cmb.DropDownStyle = ComboBoxStyle.DropDownList;
					cmb.Sorted = true;
					foreach (object obj in action.Action.List) cmb.Items.Add(obj);
					cmb.SelectedItem = action.ParamEntry;
					cmb.Bounds = e.CellBounds;
					e.Control = cmb;
				}
			}
			// offset
			else if(e.SubItemIndex == 3)
			{
				if (action.Action.ParamType != ScriptParamType.TechnoType)
				{
					e.Cancel = true;
					return;
				}

				ComboBox cmb = new ComboBox();
				cmb.FlatStyle = FlatStyle.Flat;
				cmb.DropDownStyle = ComboBoxStyle.DropDownList;
				foreach (string s in ScriptAction.OffsetDescriptions()) cmb.Items.Add(s);
				cmb.SelectedIndex = (int)action.Offset;
				cmb.Bounds = e.CellBounds;
				e.Control = cmb;
			}
		}

		private void olvSTActions_CellEditFinishing(object sender, BrightIdeasSoftware.CellEditEventArgs e)
		{
			if (e.Cancel) return;

			ScriptAction action = e.RowObject as ScriptAction;

			// action
			if (e.SubItemIndex == 1)
			{
				ComboBox cmb = e.Control as ComboBox;
				e.NewValue = cmb.SelectedItem;
			}
			// parameter
			else if(e.SubItemIndex == 2)
			{
				if(action.Action.ParamType == ScriptParamType.Number)
				{
					NumericUpDown nud = e.Control as NumericUpDown;
					action.Param = (uint)nud.Value;
				}
				else
				{
					ComboBox cmb = e.Control as ComboBox;
					action.Param = (cmb.SelectedItem as IParamListEntry).ParamListIndex;
				}

				e.Cancel = true;
				olvSTActions.RefreshItem(e.ListViewItem);
			}
			// offset
			else if(e.SubItemIndex == 3)
			{
				ComboBox cmb = e.Control as ComboBox;
				action.Offset = (uint)cmb.SelectedIndex;
				e.Cancel = true;
				olvSTActions.RefreshItem(e.ListViewItem);
			}
		}

		private void olvSTActions_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateSTActionDescription();
		}


		private void olvSTActions_CellEditFinished(object sender, BrightIdeasSoftware.CellEditEventArgs e)
		{
			UpdateSTActionDescription();
		}

		private void mnuNewST_Click(object sender, EventArgs e)
		{
			InputBox.InputResult res = InputBox.Show("New Script Type", "Enter name:");

			if (res.ReturnCode == DialogResult.OK)
			{
				string id = nextID();
				ScriptType st = new ScriptType(id, res.Text);
				scriptTypes.Add(st);
				olvST.BeginUpdate();
				olvST.AddObject(st);
				olvST.EndUpdate();
				olvST.SelectedObject = st;
			}
		}

		private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			DelActiveST();
		}

		private void olvTFUnits_SelectedIndexChanged(object sender, EventArgs e)
		{
			TaskForceEntry tfe = olvTFUnits.SelectedObject as TaskForceEntry;
			if(tfe != null) cmbTFUnit.SelectedItem = tfe.Unit;
		}

		private void mnuSTActionUp_Click(object sender, EventArgs e)
		{
			STActionMoveUp();
		}
		

		private void mnuSTActionDown_Click(object sender, EventArgs e)
		{
			STActionMoveDown();
		}

		private void mnuSTActionNew_Click(object sender, EventArgs e)
		{
			STActionNew();
		}


		private void mnuSTActionDelete_Click(object sender, EventArgs e)
		{
			STActionDelete();
		}

		private void olvSTActions_KeyDown(object sender, KeyEventArgs e)
		{
			switch(e.KeyCode)
			{
				case Keys.PageUp:
					STActionMoveUp();
					e.Handled = true;
					break;
				case Keys.PageDown:
					STActionMoveDown();
					e.Handled = true;
					break;
				case Keys.Insert:
					STActionNew();
					break;
				case Keys.Delete:
					STActionDelete();
					break;
			}
		}
	}
}
