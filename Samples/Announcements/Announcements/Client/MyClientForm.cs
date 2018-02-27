// © 2010 IDesign Inc. All rights reserved 
//Questions? Comments? go to 
//http://www.idesign.net

using System;
using System.ServiceModel.Discovery;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Linq;
using System.Collections.Generic;

[ServiceContract]
interface IMyContract
{
   [OperationContract]
   void MyMethod();
}


partial class DiscoveryForm : System.Windows.Forms.Form
{
   public DiscoveryForm()
   {
      InitializeComponent();
   }

   void OnDiscoverAddress(object sender,EventArgs e)
   {
      EndpointAddress address = DiscoverAddress<IMyContract>();
      Binding binding = new NetTcpBinding();

      IMyContract proxy = ChannelFactory<IMyContract>.CreateChannel(binding,address);
      proxy.MyMethod();

      (proxy as ICommunicationObject).Close();
   }

   void OnDiscoverMEX(object sender,EventArgs e)
   {
      IMyContract proxy = CreateChannel<IMyContract>();
      proxy.MyMethod();

      (proxy as ICommunicationObject).Close();
   }

   EndpointAddress DiscoverAddress<T>()
   {
      DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
      FindCriteria criteria = new FindCriteria(typeof(T));
      FindResponse discovered = discoveryClient.Find(criteria);

      Debug.Assert(discovered.Endpoints.Count == 1);

     return discovered.Endpoints[0].Address;
   }
   EndpointAddress[] DiscoverAddresses<T>()
   {
      DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
      FindCriteria criteria = new FindCriteria(typeof(T));
      FindResponse discovered = discoveryClient.Find(criteria);

     return discovered.Endpoints.Select((endpoint)=>endpoint.Address).ToArray();
   }
      
   Binding DiscoverBinding<T>()
   {
      Binding binding = null;
      
      DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
      FindResponse discovered = discoveryClient.Find(FindCriteria.CreateMexEndpointCriteria());

      foreach(EndpointDiscoveryMetadata mexEndpoint in discovered.Endpoints)
      {
         Debug.Assert(binding == null);

         ServiceEndpointCollection endpoints = MetadataResolver.Resolve(typeof(IMyContract),mexEndpoint.Address.Uri,MetadataExchangeClientMode.MetadataExchange);
         Debug.Assert(endpoints.Count == 1);

         binding = endpoints[0].Binding;
      }
      return binding;
   }
      
   T CreateChannel<T>()
   {
      Binding binding = null;
      EndpointAddress address = null;

      DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
      FindResponse discovered = discoveryClient.Find(FindCriteria.CreateMexEndpointCriteria());

      foreach(EndpointDiscoveryMetadata mexEndpoint in discovered.Endpoints)
      {
         Debug.Assert(binding == null);
         Debug.Assert(address == null);

         ServiceEndpointCollection endpoints = MetadataResolver.Resolve(typeof(IMyContract),mexEndpoint.Address.Uri,MetadataExchangeClientMode.MetadataExchange);
         Debug.Assert(endpoints.Count == 1);

         binding = endpoints[0].Binding;
         address = endpoints[0].Address;
      }
      return ChannelFactory<T>.CreateChannel(binding,address);
   }

   public T[] CreateChannels<T>(bool inferBinding = true)
   {
      if(inferBinding)
      {
         return CreateInferedChannels<T>();
      }
      else
      {
         return CreateChannelsFromMex<T>();
      }
   }

   T[] CreateChannelsFromMex<T>()
   {
      DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
      FindResponse discovered = discoveryClient.Find(FindCriteria.CreateMexEndpointCriteria());

      List<T> list = new List<T>();

      foreach(EndpointDiscoveryMetadata mexEndpoint in discovered.Endpoints)
      {
         ServiceEndpointCollection endpoints = MetadataResolver.Resolve(typeof(IMyContract),mexEndpoint.Address.Uri,MetadataExchangeClientMode.MetadataExchange);
         foreach(ServiceEndpoint endpoint in endpoints)
         {
            T proxy = ChannelFactory<T>.CreateChannel(endpoint.Binding,endpoint.Address);
            list.Add(proxy);
         }
      }
      return list.ToArray();
   }

   T[] CreateInferedChannels<T>()
   {
      DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
      FindCriteria criteria = new FindCriteria(typeof(T));
      FindResponse discovered = discoveryClient.Find(criteria);

      List<T> list = new List<T>();

      foreach(EndpointDiscoveryMetadata endpoint in discovered.Endpoints)
      {
         Binding binding = null;
         switch(endpoint.Address.Uri.Scheme)
         {  
            case "net.tcp":
            {
               binding = new NetTcpBinding(SecurityMode.Transport,true);
               break;
            }            
            case "net.pipe":
            {
               binding = new NetNamedPipeBinding();
               break;
            }
            case "net.msmq":
            {
               NetMsmqBinding msmqBinding = new NetMsmqBinding();
               msmqBinding.Security.Transport.MsmqProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
               binding = msmqBinding;
               break;
            }
            default:
            {
               throw new InvalidOperationException("Can only create a channel over TCP/IPC/MSMQ bindings"); 
            }
         }
         T proxy = ChannelFactory<T>.CreateChannel(binding,endpoint.Address);
         list.Add(proxy);
      }
      return list.ToArray();
   }
}