using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.Metadata.ControlDataEntity;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Mobile.Metadata;
using Kingdee.BOS.Util;
using System.ComponentModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace CZ.CEEG.OAMbl.MutiBaseDataChoosed
{
    [Description("Mbl多选基础资料多次累计选择")]
    [HotUpdate]
    public class CZ_CEEG_OAMbl_MutiBaseDataChoosed : AbstractMobileBillPlugin
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            
            string key = e.Field.Key.ToString();
            switch(key)
            {
                case "FMeetingPeople":
                    Act_AddMeetPerson(e);
                    return;
            }

            base.DataChanged(e);
        }

        private List<string> pids = new List<string>();

        #region Actions
        private void Act_AddMeetPerson(DataChangedEventArgs e)
        {
            if(e.NewValue != null)
            {
                var newValue = e.NewValue as string[];
                var oldVal = e.OldValue as DynamicObjectCollection;
                foreach (var v in oldVal)
                {
                    pids.Add(v["FMeetingPeople_Id"].ToString());
                }
                foreach (var v in newValue)
                {
                    pids.Add(v);
                }
                this.View.BillModel.SetValue("FMeetingPeople", pids);
                this.View.UpdateView("FMeetingPeople");
            }
        }


        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            base.BeforeUpdateValue(e);
            string key = e.Key.ToString();
            switch (key)
            {
                case "FMeetingPeople":
                    string v = e.Value.ToString();
                    break;
            }
        }


        #endregion
    }
}
