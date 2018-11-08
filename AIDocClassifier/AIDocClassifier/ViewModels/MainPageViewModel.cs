using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;
using Microsoft.Rest;
using Plugin.Media.Abstractions;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace AIDocClassifier.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        string trainingKey = "2342342234234234";

        // public ImageSource Image { get; private set; }
        public string Tag { get; private set; }
        public ICommand TakePhotoCommand { get; }
        public ICommand CreateTagCommand { get; }
        public ICommand TrainCustomVisionCommand { get; }

        #region Projectlist
        public ObservableCollection<string> _projectList { get; set; }
        public ObservableCollection<string> ProjectList
        {
            get { return _projectList; }
            set
            {
                _projectList = value;
                RaisePropertyChanged();
            }
        }


        private object _projectSelectedFromList;
        public object ProjectSelectedFromlist
        {
            get { return _projectSelectedFromList; }
            set
            {
                _projectSelectedFromList = value;
                RaisePropertyChanged();

                var trainingApi = GetTrainingApi(trainingKey);
                getProjectName(_projectSelectedFromList.ToString());

            }
        }
        #endregion
        public ObservableCollection<TagValues> _tagIndexer { get; set; }
        public ObservableCollection<TagValues> TagIndexer
        {
            get
            {
                return _tagIndexer;
            }
            set
            {
                _tagIndexer = value;
                RaisePropertyChanged();
            }
        }


        private Guid? _projectGUID;
        public Guid? ProjectGUID
        {
            get { return _projectGUID; }
            set
            {
                _projectGUID = value;
                RaisePropertyChanged();
            }
        }

        #region Taglist
        public ObservableCollection<string> _itemsList { get; set; }
        public ObservableCollection<string> ItemsList
        {
            get { return _itemsList; }
            set
            {
                _itemsList = value;
                RaisePropertyChanged();
            }
        }

        private TagValues _itemSelectedFromList;
        public TagValues ItemSelectedFromList
        {
            get { return _itemSelectedFromList; }
            set
            {
                _isPhotoButtonVisible = true;
                RaisePropertyChanged("IsPhotoButtonVisible");
                _itemSelectedFromList = value;
                RaisePropertyChanged();
            }
        }
        #endregion


        private string _tagNameEntry;
        public string TagNameEntry
        {
            get { return _tagNameEntry; }
            set
            {
                _tagNameEntry = value;
                RaisePropertyChanged();
            }
        }

        //make api call to get tags
        private bool _isTagPickerVisible;
        public bool IsTagPickerVisible
        {
            get { return _isTagPickerVisible; }
            set
            {
                _isTagPickerVisible = value;
                RaisePropertyChanged();
            }
        }

        //make api call to get tags
        private bool _isTagButtonButtonVisible;
        public bool IsTagButtonVisible
        {
            get { return _isTagButtonButtonVisible; }
            set
            {
                _isTagButtonButtonVisible = value;
                RaisePropertyChanged();
            }
        }

        //make api call to get tags
        private bool _isPhotoButtonVisible;
        public bool IsPhotoButtonVisible
        {
            get { return _isPhotoButtonVisible; }
            set
            {
                _isPhotoButtonVisible = value;
                RaisePropertyChanged();
            }
        }

        public MainPageViewModel(INavigationService navigationService)
            : base(navigationService)
        {
            TakePhotoCommand = new Command(async () => await TakePhoto());
            CreateTagCommand = new Command(async () => await CreateTag());
            TrainCustomVisionCommand = new Command(async () => await TrainAI());

            Start(trainingKey);

            Title = "AAG AI DOCUMENT TRAINER";

            _isTagPickerVisible = false;
            _isPhotoButtonVisible = false;
            _isTagButtonButtonVisible = false;
            RaisePropertyChanged("IsPhotoButtonVisible");
            RaisePropertyChanged("IsTagButtonVisible");
        }

        private async void getProjectName(string projName)
        {

            Guid _defaultGUID = Guid.Empty;

            var trainingApi = GetTrainingApi(trainingKey);
            var project = await GetOrCreateProject(trainingApi, projName);

            if (project.Id != null)
            {
                _defaultGUID = project.Id;
            }

            // if (project.Id != null && trainingApi != null)
            //{
            await ListProjectTags(trainingApi, _defaultGUID);
            // }

        }

        private async Task TrainAI()
        {
            try
            {
                var trainingApi = GetTrainingApi(trainingKey);
                var project = await GetOrCreateProject(trainingApi, _projectSelectedFromList.ToString());
                var iteration = trainingApi.TrainProject(project.Id);

                while (iteration.Status == "Training")
                {
                    iteration = await trainingApi.GetIterationAsync(project.Id, iteration.Id);
                }

                iteration.IsDefault = true;
                trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);

            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("No Training Required", "There are currently no new images to train. Please try again later.", "Try Again", "Cancel");
            }
        }

        private async Task CreateTag()
        {

            var trainingApi = GetTrainingApi(trainingKey);
            var project = await GetOrCreateProject(trainingApi, _projectSelectedFromList.ToString());
            var imageTag = await GetOrCreateTag(trainingApi, project.Id, _tagNameEntry);
        }

        private async Task TakePhoto()
        {
            IList<string> sendTag = new List<string>();
            string customVisionTagID = string.Empty;

            RaisePropertyChanged();
            var trainingApi = GetTrainingApi(trainingKey);
            var project = await GetOrCreateProject(trainingApi, _projectSelectedFromList.ToString());

            if (!string.IsNullOrEmpty(this._itemSelectedFromList.TagID.ToString()))
            {
                customVisionTagID = this._itemSelectedFromList.TagID.ToString();
            }

            if (!string.IsNullOrEmpty(customVisionTagID))
            {
                sendTag.Add(customVisionTagID);
            }

            var file = await Plugin.Media.CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions()
            {
                CompressionQuality = 75,
                CustomPhotoSize = 50,
                PhotoSize = PhotoSize.MaxWidthHeight,
                MaxWidthHeight = 800,
                DefaultCamera = CameraDevice.Rear,
                RotateImage = false

            });

            var response = trainingApi.CreateImagesFromData(project.Id, file.GetStream(), sendTag);
        }


        private async void Start(string trainingKey)
        {
            var trainingApi = GetTrainingApi(trainingKey);

            try
            {

                await ListProjects(trainingApi);

            }
            catch (Exception ex)
            {

                if (ex is HttpOperationException)
                {
                    //   Console.WriteLine(((HttpOperationException)ex).Response.Content);
                }
            }
        }

        private TrainingApi GetTrainingApi(string trainingKey)
        {
            return new TrainingApi
            {
                ApiKey = trainingKey
            };
        }

        private async Task ListProjects(TrainingApi trainingApi)
        {
            ObservableCollection<string> completeProjectList = new ObservableCollection<string>();
            var projects = await trainingApi.GetProjectsAsync();

            foreach (var project in projects)
            {

                completeProjectList.Add(project.Name);
            }

            _projectList = completeProjectList;
            RaisePropertyChanged("ProjectList");

            if (_projectList.Count > 0)
            {
                _isTagPickerVisible = false;
                RaisePropertyChanged("IsTagPickerVisible");
            }
        }

        public class TagValues
        {
            public string TagName { get; set; }
            public string TagID { get; set; }

        }


        private async Task ListProjectTags(TrainingApi trainingApi, Guid projectId)
        {
            var tagList = await trainingApi.GetTagsAsync(projectId);
            _tagIndexer = new ObservableCollection<TagValues>();


            foreach (var tag in tagList.Tags)
            {
                TagIndexer.Add(new TagValues
                {
                    TagName = tag.Name.ToString(),
                    TagID = tag.Id.ToString()
                });
            }

            _tagIndexer = TagIndexer;

            RaisePropertyChanged("ItemsList");
            RaisePropertyChanged("TagIndexer");
            _isTagPickerVisible = true;
            _isTagButtonButtonVisible = true;
            RaisePropertyChanged("IsTagPickerVisible");
            RaisePropertyChanged("IsTagButtonVisible");
        }

        private async Task<Project> GetOrCreateProject(TrainingApi trainingApi, string name)
        {
            var projects = await trainingApi.GetProjectsAsync();
            var project = projects.Where(p => p.Name.ToUpper() == name.ToUpper()).SingleOrDefault();

            if (project == null)
            {
                project = await trainingApi.CreateProjectAsync(name);
            }

            return project;
        }

        private async Task<Tag> GetOrCreateTag(TrainingApi trainingApi, Guid projectId, string name)
        {
            var tagList = await trainingApi.GetTagsAsync(projectId);
            var tag = tagList.Tags.Where(t => t.Name.ToUpper() == name.ToUpper()).SingleOrDefault();

            if (tag == null)
            {
                tag = await trainingApi.CreateTagAsync(projectId, name);
            }

            return tag;
        }
    }
}
