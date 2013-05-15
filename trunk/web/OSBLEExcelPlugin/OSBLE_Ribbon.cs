// Created 5-13-13 by Evan Olds for the OSBLE project at WSU
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Office.Tools.Ribbon;
using Microsoft.Office.Interop.Excel;

namespace OSBLEExcelPlugin
{
    public partial class OSBLE_Ribbon
    {
        private OSBLEState m_state = null;

        private void OSBLE_Ribbon_Load(object sender, RibbonUIEventArgs e) { }

        private void btnErrorMsg_Click(object sender, RibbonControlEventArgs e)
        {
            string s = btnErrorMsg.Tag as string;
            if (string.IsNullOrEmpty(s))
            {
                // Shouldn't ever happen
                s = "(no further information available)";
            }
            System.Windows.Forms.MessageBox.Show(
                s, "OSBLE Error Details",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }

        private void btnLogin_Click(object sender, RibbonControlEventArgs e)
        {
            m_state = null;
            using (LoginForm lf = new LoginForm())
            {
                m_state = lf.DoPrompt();
                if (null == m_state)
                {
                    return;
                }
            }

            // Put the courses in the drop-down
            ddCourses.Items.Clear();
            foreach (OSBLEServices.Course c in m_state.Courses)
            {                
                RibbonDropDownItem item 
                      = Globals.Factory.GetRibbonFactory().CreateRibbonDropDownItem();
                item.Label = c.Name;
                item.Tag = c;
                ddCourses.Items.Add(item);
            }

            if (ddCourses.Items.Count > 0)
            {
                ddCourses.SelectedItemIndex = 0;
                grpCourseGrades.Visible = true;
                btnLogin.Visible = false;
                btnLogOut.Visible = true;
                ddCourses.Visible = true;
                btnSaveToOSBLE.Visible = true;
                lblLastSave.Label = "Last save: (none since login)";
            }
            else // No courses that can be graded
            {
                grpCourseGrades.Visible = true;
                btnLogin.Visible = false;
                btnLogOut.Visible = true;
                ddCourses.Visible = false;
                btnSaveToOSBLE.Visible = false;
                lblLastSave.Label = "No courses were found for which you can enter grades";
            }
        }

        private void btnLogOut_Click(object sender, RibbonControlEventArgs e)
        {
            m_state = null;
            btnLogOut.Visible = false;
            grpCourseGrades.Visible = false;
            grpError.Visible = false;
            btnLogin.Visible = true;
        }

        private void btnSaveToOSBLE_Click(object sender, RibbonControlEventArgs e)
        {
            // We need a valid state for login
            if (null == m_state)
            {
                btnErrorMsg.Label = "Save error: You must log in first";
                btnErrorMsg.Tag = "You must log in to OSBLE with your account before you " +
                    "can upload gradebook data.";
                grpError.Visible = true;
                return;
            }

            RibbonDropDownItem rddi = ddCourses.SelectedItem;
            OSBLEServices.Course c = rddi.Tag as OSBLEServices.Course;
            OSBLEWorkbookSaver.SaveResult sr = OSBLEWorkbookSaver.Save(
                m_state.UserName, m_state.Password, c.ID,
                Globals.ThisAddIn.Application.ActiveWorkbook);

            if (!sr.Success)
            {
                btnErrorMsg.Label = "Upload attempt at " + DateTime.Now.ToString() +
                    " failed. Click here for more information.";
                btnErrorMsg.Tag = sr.ErrorMessage;
                grpError.Visible = true;
                return;
            }

            // If we did succeed then hide the error message
            grpError.Visible = false;

            lblLastSave.Label = "Last Save: " + DateTime.Now.ToString();
        }
    }
}
