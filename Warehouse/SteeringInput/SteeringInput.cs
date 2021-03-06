﻿using Database;
using System;
using System.Collections.Generic;
using System.Windows.Data;
using Warehouse.Model;

namespace Warehouse.SteeringInput
{
    public class SteeringCommands
    {
        public BasicWarehouse Warehouse { get; set; }
        public List<Action<bool, bool, bool>> SteeringNotify;

        private bool _remote;
        private bool _automatic;
        private bool _run;
        private object _lock;

        // WMS active
        public bool RemoteMode
        {
            get { return _remote; }
            set
            {
                if (_remote != value)
                {
                    _remote = value;
                    try
                    {
                        Warehouse?.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("Remote mode switched to {0}", _remote));
                        SteeringNotifyAndRemove();
                    }
                    catch
                    { }
                }
            }
        }

        // automatic
        public bool AutomaticMode
        {
            get { return _automatic; }
            set
            {
                if (_automatic != value)
                {
                    _automatic = value;
                    try
                    {
                        Warehouse?.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("Automatic mode switched to {0}", _automatic));
                        SteeringNotifyAndRemove();
                    }
                    catch
                    { }
                }
            }
        }

        private void SteeringNotifyAndRemove()
        {
            var list = new List<Action<bool, bool, bool>>();
            foreach (var prop in SteeringNotify)
            {
                try
                {
                    prop(RemoteMode, AutomaticMode, Run);
                }
                catch
                {
                    list.Add(prop);
                }
            }
            list.ForEach(p => SteeringNotify.Remove(p));
        }


        public void DirectVMNotify()
        {
            SteeringNotifyAndRemove();
        }

        // process runs
        public bool Run
        {
            get { return _run; }
            set
            {
                if (_run != value)
                {
                    _run = value;
                    try
                    {
                        Warehouse?.AddEvent(Event.EnumSeverity.Event, Event.EnumType.Program, String.Format("Run mode switched to {0}", _run));
                        SteeringNotifyAndRemove();
                    }
                    catch { }
                }
            }
        }


        public SteeringCommands()
        {
            SteeringNotify = new List<Action<bool, bool, bool>>();
            _lock = new object();
            BindingOperations.EnableCollectionSynchronization(SteeringNotify, _lock);
        }

        public void Startup()
        {
            RemoteMode = false;
            AutomaticMode = true;
            Run = false;
        }

        public void Initialize(BasicWarehouse w)
        {
            Warehouse = w;
        }


    }
}
