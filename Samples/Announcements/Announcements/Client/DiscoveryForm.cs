// © 2010 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using ServiceModelEx;
using System.ServiceModel;
using System.ServiceModel.Channels;

[ServiceContract]
interface IMyContract
{
   [OperationContract]
   void MyMethod();
}


partial class DiscoveryForm : System.Windows.Forms.Form
{
   AnnouncementSink<IMyContract> m_AnnouncementSink;
 
   public DiscoveryForm()
   {
      m_AnnouncementSink = new AnnouncementSink<IMyContract>();
      m_AnnouncementSink.Open();

      InitializeComponent();
   }
   void OnCallService(object sender,EventArgs e)
   {  
      EndpointAddress address = m_AnnouncementSink[0];
      IMyContract proxy = ChannelFactory<IMyContract>.CreateChannel(new NetTcpBinding(),address);
      proxy.MyMethod();

      (proxy as ICommunicationObject).Close();
   }

   void OnFormClosed(object sender,System.Windows.Forms.FormClosedEventArgs args)
   {
      m_AnnouncementSink.Close();
   }
}