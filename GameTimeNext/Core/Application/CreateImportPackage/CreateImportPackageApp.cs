using GameTimeNext.Core.Application.CreateImportPackage.Controller;
using GameTimeNext.Core.Application.CreateImportPackage.Views;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase;
using GameTimeNext.Core.Framework.DataBase.Migration;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.CreateImportPackage
{
    [UIXAppIcon(UIXMdlIcons.Folder)]
    public class CreateImportPackageApp : UIXApplication, IUIXApplicationStarter
    {
        public CreateImportPackageView? CreateImportPackageView { get; set; }

        public CreateImportPackageViewController? CreateImportPackageViewController { get; set; }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(CreateImportPackageApp));
        }

        public override void InitializeApplicationOutput()
        {
            CreateImportPackageView = new CreateImportPackageView();
            MainView = CreateImportPackageView;

            CreateImportPackageViewController = new CreateImportPackageViewController(this);
            CreateImportPackageView.ViewController = CreateImportPackageViewController;
        }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            Start(hostApplication, new UIXApplicationStartOptions
            {
                Target = UIXApplicationStartTarget.ContentPresenter,
                Presenter = presenter
            });
        }

        public void Start(UIXApplication hostApplication, UIXApplicationStartOptions options)
        {
            HostApplication = hostApplication;
            Loader = hostApplication.Loader;
            CallDispatcher = hostApplication.CallDispatcher;

            CreateImportPackageView!.ViewController.Show(options);
        }

        public override bool CanClose()
        {
            return true;
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }

        public void StartTestImportPackage(bool useProdDb)
        {
            // Get current ImportPackage from directory
            string importPackageFilePath;
            {
                DirectoryInfo importPackagesDirInfo = new DirectoryInfo(AppConfig.Dev.ImportPackagesDirectoryPath);
                List<FileInfo> fileInfos = importPackagesDirInfo.GetFiles("Import_Package_*.zip").ToList();
                if (fileInfos.Count != 1) throw new InvalidDataException("Number of available import packages invalid.");
                importPackageFilePath = fileInfos[0].FullName;
            }

            // Copy running DB
            string destinationDbFilePath;
            {
                string sourceDbFilePath = useProdDb ? AppConfig.Storage.NormalDatabaseFilePath : AppConfig.Storage.DatabaseFilePath;
                destinationDbFilePath = AppConfig.Dev.ImportPackageTestFilePath;
                File.Copy(sourceDbFilePath, destinationDbFilePath, true);
            }

            // Import the package
            string importPackageCsvDirPath;
            SQLiteConnection tempDb;
            {
                DataBaseManager tempDbManager = new DataBaseManager();
                tempDbManager.Initialize(destinationDbFilePath);
                tempDb = tempDbManager.GetConnection();

                importPackageCsvDirPath = Path.Combine(AppConfig.Dev.ImportPackageTestDirectoryPath, "csv");
                FnDirectory.EnsureDirectoryExists(importPackageCsvDirPath);
                ZipFile.ExtractToDirectory(importPackageFilePath, importPackageCsvDirPath);
            }

            MigrationFactory.FromCsv.MigrateTables(MigrationFactory.ImportType.ImportPackages, connection: tempDb, importDirectoryPathOverride: importPackageCsvDirPath);

            // OFDO: Auto-Detect differences

            tempDb.Close();
            tempDb.Dispose();
            tempDb = null!;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(50);
        }

        public void EndTestImportPackage()
        {
            // Delete temp DB
            Directory.Delete(AppConfig.Dev.ImportPackageTestDirectoryPath, true);
        }
    }
}
