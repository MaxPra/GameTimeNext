
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    internal class ProfilesDetailSubViewController : UIXViewControllerBase
    {

        ProfilesSubViewDataWrapper? _dataWrapper;

        public ProfilesDetailSubViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<ProfilesSubViewDataWrapper>();

            if (_dataWrapper == null)
                return;

            AddIdentifier("T1PROFI", _dataWrapper.GetTypedTableObject());
        }

        protected override void Build()
        {

        }

        protected override void BuildFirst()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void Check()
        {
        }

        protected override void FillViewImpl()
        {
            // Total game time
            FillTotalGameTime();

            // First und Lastplayed
            FillFirstLastDatePlayed();
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }
        private void FillTotalGameTime()
        {
            UIXQuery query = BuildGameTimeQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    GetView().txTotalGameTime.Text = CFProfilesApp.FormatGameTime(UIXQuery.GetDouble(reader, "TotalPlaytime"));
                }
            }
        }

        private void FillFirstLastDatePlayed()
        {
            UIXQuery query = BuildFirstLastDatePlayedQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    GetView().txFirstTimePlayed.Text = CFProfilesApp.FormatFirstLastDate(UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.FIPL));
                    GetView().txLastTimePlayed.Text = CFProfilesApp.FormatFirstLastDate(UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.LAPL));
                }
            }
        }

        private UIXQuery BuildFirstLastDatePlayedQuery()
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Felder
            query.AddField(K1PROFI.Name, K1PROFI.Fields.FIPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LAPL);

            // Where Restriktionen
            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.PFID, QueryCompareType.EQUALS, _dataWrapper!.GetTypedTableObject().PFID);

            return query;
        }

        private UIXQuery BuildGameTimeQuery()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Felder hinzufügen
            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");

            // Where Restriktionen
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, _dataWrapper!.GetTypedTableObject().PFID);

            return query;
        }

        private ProfilesDetailView GetView()
        {
            return (ProfilesDetailView)this.View;
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {

        }
    }
}
