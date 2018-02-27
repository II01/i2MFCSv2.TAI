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
         System.Windows.Forms.Button addressButton;
         System.Windows.Forms.Button addressBindingButton;
         addressButton = new System.Windows.Forms.Button();
         addressBindingButton = new System.Windows.Forms.Button();
         this.SuspendLayout();
         // 
         // addressButton
         // 
         addressButton.Location = new System.Drawing.Point(47,26);
         addressButton.Name = "addressButton";
         addressButton.Size = new System.Drawing.Size(128,23);
         addressButton.TabIndex = 0;
         addressButton.Text = "Discover Address";
         addressButton.UseVisualStyleBackColor = true;
         addressButton.Click += new System.EventHandler(this.OnDiscoverAddress);
         // 
         // addressBindingButton
         // 
         addressBindingButton.Location = new System.Drawing.Point(47,75);
         addressBindingButton.Name = "addressBindingButton";
         addressBindingButton.Size = new System.Drawing.Size(128,23);
         addressBindingButton.TabIndex = 0;
         addressBindingButton.Text = "Discover from MEX";
         addressBindingButton.UseVisualStyleBackColor = true;
         addressBindingButton.Click += new System.EventHandler(this.OnDiscoverMEX);
         // 
         // MyClientForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F,13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(218,138);
         this.Controls.Add(addressBindingButton);
         this.Controls.Add(addressButton);
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "MyClientForm";
         this.Text = "Discovery Demo";
         this.ResumeLayout(false);

      }

      #endregion

   }
