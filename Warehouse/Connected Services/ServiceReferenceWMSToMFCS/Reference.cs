﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Warehouse.ServiceReferenceWMSToMFCS {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="ServiceReferenceWMSToMFCS.IWMSToMFCS")]
    public interface IWMSToMFCS {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWMSToMFCS/CommandStatusChanged", ReplyAction="http://tempuri.org/IWMSToMFCS/CommandStatusChangedResponse")]
        void CommandStatusChanged(int cmdId, int status);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWMSToMFCS/CommandStatusChanged", ReplyAction="http://tempuri.org/IWMSToMFCS/CommandStatusChangedResponse")]
        System.Threading.Tasks.Task CommandStatusChangedAsync(int cmdId, int status);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWMSToMFCS/PlaceChanged", ReplyAction="http://tempuri.org/IWMSToMFCS/PlaceChangedResponse")]
        void PlaceChanged(string placeID, int TU_ID, string changeType);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWMSToMFCS/PlaceChanged", ReplyAction="http://tempuri.org/IWMSToMFCS/PlaceChangedResponse")]
        System.Threading.Tasks.Task PlaceChangedAsync(string placeID, int TU_ID, string changeType);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWMSToMFCS/DestinationEmptied", ReplyAction="http://tempuri.org/IWMSToMFCS/DestinationEmptiedResponse")]
        void DestinationEmptied(string place);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWMSToMFCS/DestinationEmptied", ReplyAction="http://tempuri.org/IWMSToMFCS/DestinationEmptiedResponse")]
        System.Threading.Tasks.Task DestinationEmptiedAsync(string place);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWMSToMFCS/OrderForRampActive", ReplyAction="http://tempuri.org/IWMSToMFCS/OrderForRampActiveResponse")]
        bool OrderForRampActive(string ramp);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IWMSToMFCS/OrderForRampActive", ReplyAction="http://tempuri.org/IWMSToMFCS/OrderForRampActiveResponse")]
        System.Threading.Tasks.Task<bool> OrderForRampActiveAsync(string ramp);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IWMSToMFCSChannel : Warehouse.ServiceReferenceWMSToMFCS.IWMSToMFCS, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class WMSToMFCSClient : System.ServiceModel.ClientBase<Warehouse.ServiceReferenceWMSToMFCS.IWMSToMFCS>, Warehouse.ServiceReferenceWMSToMFCS.IWMSToMFCS {
        
        public WMSToMFCSClient() {
        }
        
        public WMSToMFCSClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public WMSToMFCSClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public WMSToMFCSClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public WMSToMFCSClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public void CommandStatusChanged(int cmdId, int status) {
            base.Channel.CommandStatusChanged(cmdId, status);
        }
        
        public System.Threading.Tasks.Task CommandStatusChangedAsync(int cmdId, int status) {
            return base.Channel.CommandStatusChangedAsync(cmdId, status);
        }
        
        public void PlaceChanged(string placeID, int TU_ID, string changeType) {
            base.Channel.PlaceChanged(placeID, TU_ID, changeType);
        }
        
        public System.Threading.Tasks.Task PlaceChangedAsync(string placeID, int TU_ID, string changeType) {
            return base.Channel.PlaceChangedAsync(placeID, TU_ID, changeType);
        }
        
        public void DestinationEmptied(string place) {
            base.Channel.DestinationEmptied(place);
        }
        
        public System.Threading.Tasks.Task DestinationEmptiedAsync(string place) {
            return base.Channel.DestinationEmptiedAsync(place);
        }
        
        public bool OrderForRampActive(string ramp) {
            return base.Channel.OrderForRampActive(ramp);
        }
        
        public System.Threading.Tasks.Task<bool> OrderForRampActiveAsync(string ramp) {
            return base.Channel.OrderForRampActiveAsync(ramp);
        }
    }
}
