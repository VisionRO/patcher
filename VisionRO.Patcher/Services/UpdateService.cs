using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VisionRO.Patcher.Services
{
    public class UpdateService
    {
        private enum StateEnum { Idle, Installing, Updating, Reparing };
        private readonly string _localPath;
        private readonly string _remoteUrl;
        private readonly Action<string, int> _updateProgressDelegate;
        private readonly Action _clientReadyDelegate;
        private StateEnum _state = StateEnum.Idle;

        public UpdateService(string localPath, string remoteUrl, Action<string, int> updateProgressDelegate, Action clientReadyDelegate)
        {
            _localPath = localPath;
            _remoteUrl = remoteUrl;
            _updateProgressDelegate = updateProgressDelegate;
            _clientReadyDelegate = clientReadyDelegate;
        }

        public bool IsInstalled()
        {
            try
            {
                new Repository(_localPath).Dispose();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task InstallAsync()
        {
            return Task.Run(() =>
            {
                Install();
            });
        }

        private void Install()
        {
            if (_state != StateEnum.Idle) return;
            _state = StateEnum.Installing;
            try
            {
                Repository.Clone(_remoteUrl, _localPath, new CloneOptions
                {
                    Checkout = true,
                    BranchName = "master",
                    OnTransferProgress = OnTransferProgress,
                    OnProgress = OnProgress,
                    OnCheckoutProgress = OnCheckoutProgress
                });
                _updateProgressDelegate("Installation completed!", 100);
                _clientReadyDelegate();
            }
            catch (Exception ex)
            {
                _updateProgressDelegate("An error occured...", 0);
                File.WriteAllText("vision-patcher.log", JsonConvert.SerializeObject(ex));
            }
            finally
            {
                _state = StateEnum.Idle;
            }
        }

        public Task UpdateAsync()
        {
            return Task.Run(() =>
            {
                Update();
            });
        }

        private void Update()
        {
            if (_state != StateEnum.Idle) return;
            _state = StateEnum.Updating;
            try
            {
                using (var repo = new Repository(_localPath))
                {
                    Commands.Pull(repo, new Signature("any", "any", DateTime.Now), new PullOptions
                    {
                        FetchOptions = new FetchOptions
                        {
                            OnProgress = OnProgress,
                            OnTransferProgress = OnTransferProgress,
                        },
                        MergeOptions = new MergeOptions
                        {
                            CommitOnSuccess = false,
                            FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                            OnCheckoutProgress = OnCheckoutProgress
                        }
                    });
                }
                _updateProgressDelegate("Update completed!", 100);
                _clientReadyDelegate();
            }
            catch (Exception ex)
            {
                _updateProgressDelegate("An error occured...", 0);
                File.WriteAllText("vision-patcher.log", JsonConvert.SerializeObject(ex));
            }
            finally
            {
                _state = StateEnum.Idle;
            }
        }

        public Task RepairAsync()
        {
            return Task.Run(() =>
            {
                Repair();
            });
        }

        private void Repair()
        {
            if (_state != StateEnum.Idle) return;
            _state = StateEnum.Reparing;
            try
            {
                using (var repo = new Repository(_localPath))
                {
                    repo.Reset(ResetMode.Hard, "master");
                }
                _updateProgressDelegate("Repair completed!", 100);
                _state = StateEnum.Idle;
                Update();
            }
            catch (Exception ex)
            {
                _updateProgressDelegate("An error occured...", 0);
                File.WriteAllText("vision-patcher.log", JsonConvert.SerializeObject(ex));
                _state = StateEnum.Idle;
            }
        }

        private bool OnTransferProgress(TransferProgress progress)
        {
            var mbReceived = Math.Round((double)progress.ReceivedBytes / 1024 / 1024, 1);
            var pctProgress = (int)((double)progress.ReceivedObjects / progress.TotalObjects * 100);
            _updateProgressDelegate($"Downloading... {mbReceived}", pctProgress);
            return true;
        }

        private bool OnProgress(string serverProgressOutput)
        {
            _updateProgressDelegate(serverProgressOutput, 0);
            return true;
        }

        private void OnCheckoutProgress(string path, int completedSteps, int totalSteps)
        {
            _updateProgressDelegate($"Checking out {path}", (int)((double)completedSteps / totalSteps * 100));
        }
    }
}
