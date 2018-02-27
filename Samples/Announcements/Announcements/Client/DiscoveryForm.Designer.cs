// © 2010 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net



partial class DiscoveryForm
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
         if(disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         System.Windows.Forms.Button callButton;
         callButton = new System.Windows.Forms.Button();
         this.SuspendLayout();
         // 
         // callButton
         // 
         callButton.Location = new System.Drawing.Point(66,37);
         callButton.Name = "callButton";
         callButton.Size = new System.Drawing.Size(89,23);
         callButton.TabIndex = 0;
         callButton.Text = "Call Service";
         callButton.UseVisualStyleBackColor = true;
         callButton.Click += new System.EventHandler(this.OnCallService);
         // 
         // DiscoveryForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F,13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(218,98);
         this.Controls.Add(callButton);
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "DiscoveryForm";
         this.Text = "Announcements Demo";
         this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnFormClosed);
         this.ResumeLayout(false);

      }

      #endregion

   }
