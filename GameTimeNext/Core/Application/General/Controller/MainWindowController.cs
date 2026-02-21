using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;

namespace GameTimeNext.Core.Application.General.Controller
{
    internal class MainWindowController : UIXWindowControllerBase
    {

        protected override void Init()
        {
        }

        protected override void BuildFirst()
        {
        }

        protected override void Build()
        {

        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            switch (eventName)
            {
                case UIXEventNames.Selector.SelectionChanged:

                    if (source is TabControl)
                        Event_TabChanged((TabControl)source);
                    break;
            }
        }

        protected override void Check()
        {

        }

        protected override void DataWrapperSelectionChangedImpl(Selector source) { }

        protected override void Event_Closing()
        {

        }

        protected override void Event_Minimize()
        {

        }

        protected override void Event_Maximize()
        {

        }

        /// <summary>
        /// Wird Tab geändert
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TabChanged(TabControl source)
        {

            if (source.SelectedItem is TabItem ti && ti.Name == "Tab_Profiles")
                GetWindow().ProfilesSubViewController.Reopen(UIXEvents.Reopen);
        }

        private MainWindow GetWindow()
        {
            return (MainWindow)View;
        }
    }
}
