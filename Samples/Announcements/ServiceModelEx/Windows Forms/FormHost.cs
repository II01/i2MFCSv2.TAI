// � 2010 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.Windows.Forms;
using System.Threading;
using System.ServiceModel;


namespace ServiceModelEx
{
   [Serializable]
   [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
   public abstract class FormHost<F> : Form where F : Form
   {
      protected ServiceHost<F> Host
      {
         get;set;
      }
      public FormHost(params string[] baseAddresses)
      {
         Host = new ServiceHost<F>(this as F,baseAddresses);

         Load += delegate
                 {
                    if(Host.State == CommunicationState.Created)
                    {
                       Host.Open();
                    }
                 };         
         FormClosed += delegate
                       {
                          if(Host.State == CommunicationState.Opened)
                          {
                             Host.Close();
                          }
                       };
      }
   }
}