namespace OSBLEExcelPlugin
{
    partial class OSBLE_Ribbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public OSBLE_Ribbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tab1 = this.Factory.CreateRibbonTab();
            this.group1 = this.Factory.CreateRibbonGroup();
            this.btnLogin = this.Factory.CreateRibbonButton();
            this.btnLogOut = this.Factory.CreateRibbonButton();
            this.grpCourseGrades = this.Factory.CreateRibbonGroup();
            this.btnSaveToOSBLE = this.Factory.CreateRibbonButton();
            this.ddCourses = this.Factory.CreateRibbonDropDown();
            this.lblLastSave = this.Factory.CreateRibbonLabel();
            this.grpError = this.Factory.CreateRibbonGroup();
            this.btnErrorMsg = this.Factory.CreateRibbonButton();
            this.tab1.SuspendLayout();
            this.group1.SuspendLayout();
            this.grpCourseGrades.SuspendLayout();
            this.grpError.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.group1);
            this.tab1.Groups.Add(this.grpCourseGrades);
            this.tab1.Groups.Add(this.grpError);
            this.tab1.Label = "OSBLE";
            this.tab1.Name = "tab1";
            // 
            // group1
            // 
            this.group1.Items.Add(this.btnLogin);
            this.group1.Items.Add(this.btnLogOut);
            this.group1.Label = "Account";
            this.group1.Name = "group1";
            // 
            // btnLogin
            // 
            this.btnLogin.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnLogin.Image = global::OSBLEExcelPlugin.Properties.Resources.base_key_32;
            this.btnLogin.Label = "Sign in...";
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.ShowImage = true;
            this.btnLogin.SuperTip = "Prompts you for a user name and password to sign in to OSBLE";
            this.btnLogin.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnLogin_Click);
            // 
            // btnLogOut
            // 
            this.btnLogOut.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnLogOut.Image = global::OSBLEExcelPlugin.Properties.Resources.black_x_32x32;
            this.btnLogOut.Label = "Sign out";
            this.btnLogOut.Name = "btnLogOut";
            this.btnLogOut.ShowImage = true;
            this.btnLogOut.Visible = false;
            this.btnLogOut.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnLogOut_Click);
            // 
            // grpCourseGrades
            // 
            this.grpCourseGrades.Items.Add(this.btnSaveToOSBLE);
            this.grpCourseGrades.Items.Add(this.ddCourses);
            this.grpCourseGrades.Items.Add(this.lblLastSave);
            this.grpCourseGrades.Label = "Course Grades";
            this.grpCourseGrades.Name = "grpCourseGrades";
            this.grpCourseGrades.Visible = false;
            // 
            // btnSaveToOSBLE
            // 
            this.btnSaveToOSBLE.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btnSaveToOSBLE.Image = global::OSBLEExcelPlugin.Properties.Resources.uparrow_32x32;
            this.btnSaveToOSBLE.Label = "Upload to OSBLE";
            this.btnSaveToOSBLE.Name = "btnSaveToOSBLE";
            this.btnSaveToOSBLE.ShowImage = true;
            this.btnSaveToOSBLE.SuperTip = "Uploads all of the worksheets in this workbook to the gradebook of the selected c" +
    "ourse";
            this.btnSaveToOSBLE.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnSaveToOSBLE_Click);
            // 
            // ddCourses
            // 
            this.ddCourses.Label = "Course:";
            this.ddCourses.Name = "ddCourses";
            this.ddCourses.SelectionChanged += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.ddCourses_SelectionChanged);
            // 
            // lblLastSave
            // 
            this.lblLastSave.Label = "Last save: (none since login)";
            this.lblLastSave.Name = "lblLastSave";
            // 
            // grpError
            // 
            this.grpError.Items.Add(this.btnErrorMsg);
            this.grpError.Label = "Error";
            this.grpError.Name = "grpError";
            this.grpError.Visible = false;
            // 
            // btnErrorMsg
            // 
            this.btnErrorMsg.Label = "(error message short)";
            this.btnErrorMsg.Name = "btnErrorMsg";
            this.btnErrorMsg.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btnErrorMsg_Click);
            // 
            // OSBLE_Ribbon
            // 
            this.Name = "OSBLE_Ribbon";
            this.RibbonType = "Microsoft.Excel.Workbook";
            this.Tabs.Add(this.tab1);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.OSBLE_Ribbon_Load);
            this.tab1.ResumeLayout(false);
            this.tab1.PerformLayout();
            this.group1.ResumeLayout(false);
            this.group1.PerformLayout();
            this.grpCourseGrades.ResumeLayout(false);
            this.grpCourseGrades.PerformLayout();
            this.grpError.ResumeLayout(false);
            this.grpError.PerformLayout();

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tab1;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group1;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnLogin;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnSaveToOSBLE;
        internal Microsoft.Office.Tools.Ribbon.RibbonLabel lblLastSave;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpCourseGrades;
        internal Microsoft.Office.Tools.Ribbon.RibbonDropDown ddCourses;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup grpError;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnLogOut;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btnErrorMsg;
    }

    partial class ThisRibbonCollection
    {
        internal OSBLE_Ribbon OSBLE_Ribbon
        {
            get { return this.GetRibbon<OSBLE_Ribbon>(); }
        }
    }
}
