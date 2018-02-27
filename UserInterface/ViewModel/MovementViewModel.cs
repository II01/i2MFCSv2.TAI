using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using UserInterface.Services;
using Warehouse.Model;
using System.Diagnostics;
using Database;
using GalaSoft.MvvmLight.Messaging;
using UserInterface.Messages;

namespace UserInterface.ViewModel
{
    public sealed class MovementViewModel: ViewModelBase
    {
        #region members
        private Movement _movement;
        #endregion

        #region properties
        public DateTime Time
        {
            get { return _movement.Time; }
            set
            {
                if (_movement.Time != value)
                {
                    _movement.Time = value;
                    RaisePropertyChanged("Time");
                }
            }
        }
        public int ID
        {
            get { return _movement.ID; }
            set
            {
                if (_movement.ID != value)
                {
                    _movement.ID = value;
                    RaisePropertyChanged("ID");
                }
            }
        }
        public string Position
        {
            get { return _movement.Position; }
            set
            {
                if (_movement.Position != value)
                {
                    _movement.Position = value;
                    RaisePropertyChanged("Position");
                }
            }
        }
        public int Material
        {
            get { return _movement.Material; }
            set
            {
                if (_movement.Material != value)
                {
                    _movement.Material = value;
                    RaisePropertyChanged("Material");
                }
            }
        }

        public Services.EnumMovementTask Task
        {
            get { return (Services.EnumMovementTask)_movement.Task; }
            set
            {
                if (_movement.Task != (Database.EnumMovementTask)value)
                {
                    _movement.Task = (Database.EnumMovementTask)value;
                    RaisePropertyChanged("Task");
                }
            }
        }

        public Movement Movement
        {
            get { return _movement; }
            set
            {
                if(_movement != value)
                {
                    _movement = value;
                    RaisePropertyChanged("Movement");
                }
            }
        }

        #endregion

        #region intialization    
        public MovementViewModel()
        {
            _movement = new Movement();
        }
        public void Initialize(BasicWarehouse warehouse)
        {
        }

        #endregion
    }
}
