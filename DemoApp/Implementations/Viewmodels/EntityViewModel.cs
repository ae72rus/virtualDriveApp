using System;
using DemoApp.Abstractions.Common;
using DemoApp.Abstractions.Services;
using DemoApp.Abstractions.Viewmodels;
using DemoApp.Enums;
using DemoApp.Implementations.Common;
using VirtualDrive;

namespace DemoApp.Implementations.Viewmodels
{
    public class EntityViewModel : BaseViewmodel, IEntityViewModel
    {
        private readonly IEntityInfo _entityInfo;
        private bool _isRenaming;
        private string _editableName;
        private bool _isCut;

        public EntityViewModel(IWindowsManager windowsManager, IVirtualFileSystem fs, VirtualDirectory directory, bool isParentDirectory)
            : base(windowsManager)
        {
            _entityInfo = isParentDirectory
                ? new ParentDirectoryInfo(directory, fs)
                : new VirtualDirectoryInfo(directory, fs);

            EditableName = Name;
        }
        public EntityViewModel(IWindowsManager windowsManager, IVirtualFileSystem fs, VirtualFile file)
            : base(windowsManager)
        {
            _entityInfo = new VirtualFileInfo(file, fs);
            EditableName = Name;
        }

        public string Name => _entityInfo.Name;
        public string FullName => _entityInfo.FullName;
        public EntityType Type => _entityInfo.Type;
        public DateTime CreatedDateTime => _entityInfo.CreatedDateTime;
        public DateTime ModifiedDateTime => _entityInfo.ModifiedDateTime;
        public IVirtualFileSystem VirtualFileSystem => _entityInfo.VirtualFileSystem;

        public string EditableName
        {
            get => _editableName;
            set
            {
                _editableName = value;
                RaisePropertyChanged();
            }
        }

        public bool IsRenaming
        {
            get => _isRenaming;
            set
            {
                _isRenaming = value;
                RaisePropertyChanged();

                if (_isRenaming || EditableName.Equals(Name, StringComparison.InvariantCultureIgnoreCase))
                    return;

                try
                {
                    _entityInfo.Name = EditableName;
                }
                catch (Exception e)
                {
                    WindowsManager.ReportError("Rename error", e);
                }
                finally
                {
                    RaisePropertyChanged(() => EditableName);
                    RaisePropertyChanged(() => Name);
                    RaisePropertyChanged(() => FullName);
                }
            }
        }

        public bool IsCut
        {
            get => _isCut;
            set
            {
                _isCut = value;
                RaisePropertyChanged();
            }
        }

        public void RaiseUpdated()
        {
            RaisePropertyChanged(() => Name);
            RaisePropertyChanged(() => FullName);
            RaisePropertyChanged(() => CreatedDateTime);
            RaisePropertyChanged(() => ModifiedDateTime);
        }

        public IEntityInfo GetEntityInfo()
        {
            return _entityInfo;
        }
    }
}