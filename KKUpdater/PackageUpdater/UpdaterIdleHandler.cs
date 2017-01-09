#region https://github.com/mr-kelly

// KKUpdater - Robust Resources Package Downloader
// 
// A private module of KSFramework<https://github.com/mr-kelly/KSFramework>
//  
// Author: chenpeilin / mr-kelly
// Email: 23110388@qq.com
// Website: https://github.com/mr-kelly

#endregion

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     Default updater handler, do nothing but finish
    /// </summary>
    public class UpdaterIdleHandler : UpdaterHandler
    {
        protected internal override void Start()
        {
            Finish();
        }
    }
}