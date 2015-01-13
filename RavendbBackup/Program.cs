using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using Raven.Abstractions.Data;
using Raven.Client.Document;

namespace RavendbBackup
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var backupFolder = ConfigurationManager.AppSettings["BackupFolder"];

            if (String.IsNullOrEmpty(backupFolder))
            {
                System.Console.WriteLine("Please enter a backupFolder in the App Settings");
                return;
            }

            if (args.Length == 0)
            {
                System.Console.WriteLine("Please enter a numeric argument.");
                return;
            }

            if (args.Length == 1 && args[0] == "-h")
            {
                System.Console.WriteLine("-- backup databasename");
                System.Console.WriteLine("-- restore databasename backupfilename");
                //System.Console.WriteLine("add param -i for incrementel backup");
                return;
            }

            if (args.Length == 1 && args[0] == "backup")
            {
                var store = new DocumentStore() {ConnectionStringName = "ravendb"};
                store.Initialize();
                var databaseNames = store.DatabaseCommands.GlobalAdmin.GetDatabaseNames(1024);
                store.Dispose();

                //var incremental = args.Length >= 3 && args[2] == "-i";
                foreach (var databaseName in databaseNames)
                {
                    try
                    {
                        var combinedFolder = Path.Combine(backupFolder, databaseName);

                        if (Directory.Exists(combinedFolder))
                        {
                            Directory.Delete(combinedFolder, true);
                        }

                        System.Console.WriteLine("Backup " + databaseName + " started");

                        store = new DocumentStore() {ConnectionStringName = "ravendb", DefaultDatabase = databaseName};
                        store.Initialize();
                        store
                            .DatabaseCommands
                            .GlobalAdmin
                            .StartBackup(
                                combinedFolder,
                                new DatabaseDocument(),
                                incremental: false,
                                databaseName: databaseName);

                        //System.Threading.Thread.Sleep(1000);
                        var status = new RavendbBackupStatus();
                        using (var session = store.OpenSession())
                        {
                            status = session.Load<RavendbBackupStatus>("Raven/Backup/Status");
                        }
                        //var status = session.Query<dynamic>("Raven/Backup/Status");

                        //System.Console.WriteLine(status.IsRunning);
                        System.Console.WriteLine("Start " + status.Started.Value.ToString("dd.MM.yyyy HH:mm:ss"));

                        //System.Console.WriteLine(status.Completed);
                        while (status.IsRunning)
                        {
                            System.Threading.Thread.Sleep(1000);
                            using (var session = store.OpenSession())
                            {
                                status = session.Load<RavendbBackupStatus>("Raven/Backup/Status");
                            }
                            //System.Console.WriteLine(status.IsRunning);
                        }

                        System.Console.WriteLine("Ended " + status.Completed.Value.ToString("dd.MM.yyyy HH:mm:ss"));


                        store.Dispose();

                        System.Console.WriteLine("Zipping ... startet");
                        using (ZipFile zip = new ZipFile())
                        {
                            try
                            {
                                zip.AddDirectory(combinedFolder);
                                System.Console.WriteLine("Added");
                                zip.ParallelDeflateThreshold = -1;
                                zip.Save(combinedFolder + "_" + ((int) DateTime.Now.DayOfWeek) + ".zip");
                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine(ex.Message);
                            }
                        }

                        System.Console.WriteLine("Zipping ... ended");

                        if (Directory.Exists(combinedFolder))
                        {
                            Directory.Delete(combinedFolder, true);
                        }

                        System.Console.WriteLine("Backup " + databaseName + " ended");
                    }
                    catch (Exception ex)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine(ex.Message);
                        System.Console.WriteLine(ex.StackTrace);
                        System.Console.ForegroundColor = ConsoleColor.White;
                    }
                }


                return;
            }

            // Will not be used, because is really slowly. So please make the restore in the Web Dashboard
            //if (args.Length >= 2 && args[0] == "restore" && args[1] != "") {
            //    var databaseName = args[1];

            //    var combinedFolder = Path.Combine(backupFolder, databaseName);

            //    if (!Directory.Exists(combinedFolder)) {
            //        System.Console.WriteLine(combinedFolder + " Folder not exists");
            //        return;
            //    }

            //    System.Console.WriteLine("Restore started");
            //    var store = new DocumentStore() { ConnectionStringName = "ravendb", DefaultDatabase = databaseName };
            //    store.Initialize();

            //    store
            //        .DatabaseCommands
            //        .GlobalAdmin
            //        .StartRestore(
            //            new DatabaseRestoreRequest {
            //                BackupLocation = combinedFolder,
            //                DatabaseLocation = @"~\Databases\" + databaseName + @"\",
            //                DatabaseName = databaseName
            //            });

            //    store.Dispose();

            //    System.Console.WriteLine("Restore ended");
            //    return;
            //}

            System.Console.WriteLine("Command not valid use help");
        }
    }
}