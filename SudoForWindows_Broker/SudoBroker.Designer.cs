//Copyright 2023 VMware, Inc.
//SPDX-License-Identifier: BSD-2-Clause
namespace SudoForWindows_Broker
{
    partial class SudoBroker
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.eventLogSender = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.eventLogSender)).BeginInit();
            // 
            // SudoBroker
            // 
            this.ServiceName = "SudoBroker";
            ((System.ComponentModel.ISupportInitialize)(this.eventLogSender)).EndInit();

        }

        #endregion

        private System.Diagnostics.EventLog eventLogSender;
    }
}
