// © 2010 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.ServiceModel;
using System.Windows.Forms;
using System.ServiceModel.Discovery;
using ServiceModelEx;


class Program
{
   public static void Main()
   {
      Uri baseAddress = DiscoveryHelper.AvailableTcpBaseAddress;

      ServiceHost host = new ServiceHost(typeof(MyService),baseAddress);
      host.Open();

      Application.Run(new HostForm());
      
      host.Close();
   }
}






