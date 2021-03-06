﻿using Coursera;
using FSharp.Control;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace LearnOnTheGo
{
    public partial class CoursePage : PhoneApplicationPage
    {
        public CoursePage()
        {
            InitializeComponent();
            CommonMenuItems.Init(this);
        }

        private int courseId;
        private LazyBlock<LectureSection[]> lecturesLazyBlock;
        private LazyBlock<string> videoLazyBlock;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (App.Crawler == null)
            {
                // app was tombstoned or settings changed
                LittleWatson.Log("App.Crawler is null");
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
                else
                {
                    LittleWatson.Log("Can not go back");
                }
                return;
            }

            courseId = int.Parse(NavigationContext.QueryString["courseId"]);
            var course = App.Crawler.GetCourse(courseId);
            pivot.Title = course.Topic.Name;
            LittleWatson.Log(courseId + " = " + course.Topic.Name + " [" + course.Name + "]");

            Load(false);
        }

        private void Load(bool refresh)
        {
            var course = App.Crawler.GetCourse(courseId);
            if (!course.Active)
            {
                if (course.HasFinished)
                {
                    messageTextBlock.Text = "Lectures no longer available";
                }
                else
                {
                    messageTextBlock.Text = "Lectures not available yet";
                }
            }
            else
            {
                if (refresh)
                {
                    course = App.Crawler.RefreshCourse(course.Id);
                }
                lecturesLazyBlock = new LazyBlock<LectureSection[]>(
                    "lectures",
                    "No lectures available. Make sure you have accepted the honor code.",
                    course.LectureSections,
                    a => a.Length == 0,
                    new LazyBlockUI<LectureSection[]>(
                        this,
                        lectureSections => pivot.ItemsSource = lectureSections,
                        () => pivot.ItemsSource != null,
                        messageTextBlock),
                    false,
                    null,
                    null,
                    null);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (lecturesLazyBlock != null)
            {
                lecturesLazyBlock.Cancel();
            }
            if (videoLazyBlock != null)
            {
                videoLazyBlock.Cancel();
            }
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            LittleWatson.Log("OnRefreshClick");

            if (lecturesLazyBlock == null || lecturesLazyBlock.CanRefresh)
            {
                Load(true);
            }
        }

        private void OnLectureVideoClick(object sender, RoutedEventArgs e)
        {
            LittleWatson.Log("OnLectureVideoClick");

            if (videoLazyBlock != null)
            {
                LittleWatson.Log("videoLazyBlock is not null");
                return;
            }

            var lecture = (Coursera.Lecture)((Button)sender).DataContext;

            videoLazyBlock = new LazyBlock<string>(
                "video",
                null,
                lecture.VideoUrl,
                _ => false,
                new LazyBlockUI<string>(
                    this,
                    videoUrl =>
                    {
                        try
                        {
                            var launcher = new MediaPlayerLauncher();
                            launcher.Media = new Uri(videoUrl, UriKind.Absolute);
                            launcher.Show();
                        }
                        catch (Exception ex)
                        {
                            LittleWatson.ReportException(ex, string.Format("Opening video for lecture '{0}' of course '{1}' ({2})", lecture.Title, pivot.Title, videoUrl));
                            LittleWatson.CheckForPreviousException(false);
                        }
                    },
                    () => false,
                    null),
                false,
                null,
                _ => videoLazyBlock = null,
                null);
        }

        private void OnLectureNotesClick(object sender, RoutedEventArgs e)
        {
            LittleWatson.Log("OnLectureNotesClick");

            var lecture = (Coursera.Lecture)((Button)sender).DataContext;

            var task = new WebBrowserTask();
            task.Uri = new Uri(lecture.LectureNotesUrl, UriKind.Absolute);
            task.Show();
        }
    }
}