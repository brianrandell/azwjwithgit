using System.IO;
using Microsoft.Azure.WebJobs;
using LibGit2Sharp;
using System;

namespace libgit2sharpwebjob
{
  public class Functions
  {
    // This function will be triggered based on the schedule you have set for this WebJob
    // This function will enqueue a message on an Azure Queue called queue
    [NoAutomaticTrigger]
    public static void ManualTrigger(TextWriter log, int value, [Queue("queue")] out string message)
    {
      log.WriteLine("Function is invoked with value={0}", value);
      message = value.ToString();
      RunLibGit2SharpCode(log);
      log.WriteLine("Following message will be written on the Queue={0}", message);
    }

    private static void RunLibGit2SharpCode(TextWriter log)
    {
      // TODO: enter your VSTS alternate credentials
      string userName = "";
      string password = "";

      // TODO: enter your VSTS account name
      string accountRoot = "yourroot";

      // TODO: enter the name of a clean VSTS team project 
      // with an emptry default Git repo
      string teamProjectName = "yourteamproject";

      try
      {
        var co = new CloneOptions();
        co.CredentialsProvider = (_url, _user, _cred) =>
          new UsernamePasswordCredentials
          {
            Username = userName,
            Password = password
          };

        string reposRoot = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
          "repos");
        if (Directory.Exists(reposRoot))
        {
          // directory exists, delete
          log.WriteLine("Cleaning reposRoot: " + reposRoot);
          DeleteReadOnlyDirectory(reposRoot);
        }
        string repoRoot = Path.Combine(reposRoot, teamProjectName);

        string accountUrl = string.Format("https://{0}.visualstudio.com/DefaultCollection", accountRoot);

        Repository.Clone(accountUrl + "/_git/" + teamProjectName, repoRoot, co);
        log.WriteLine("Clone Done!");

        using (Repository repo = new Repository(repoRoot))
        {
          log.WriteLine("Repo aquired");

          //// Write file
          string newFilePath = System.IO.Path.Combine(repoRoot, "test.txt");
          System.IO.File.WriteAllText(newFilePath, "Some great text.");
          log.WriteLine("New text file written");

          log.WriteLine("repo.RetrieveStatus()");
          var status = repo.RetrieveStatus();
          foreach (var item in status)
          {
            if (item.State == FileStatus.Untracked)
            {
              // *** Warning ***
              // this line of code causes the Azure Web Job to blow up
              repo.Index.Add(item.FilePath);
              log.WriteLine(item.FilePath + " added to index");
            }
          }
        }
      }
      catch (Exception ex)
      {
        log.WriteLine("Failure in LoadCodeIntoGitRepo");
        log.WriteLine(ex.ToString());
      }
    }

    private static void DeleteReadOnlyDirectory(string directory)
    {
      foreach (var subdirectory in Directory.EnumerateDirectories(directory))
      {
        DeleteReadOnlyDirectory(subdirectory);
      }
      foreach (var fileName in Directory.EnumerateFiles(directory))
      {
        var fileInfo = new FileInfo(fileName);
        fileInfo.Attributes = FileAttributes.Normal;
        fileInfo.Delete();
      }
      Directory.Delete(directory);
    }
  }
}
