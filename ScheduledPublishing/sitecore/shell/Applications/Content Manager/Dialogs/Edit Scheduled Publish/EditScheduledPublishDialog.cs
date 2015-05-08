﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using ScheduledPublishing.Models;
using ScheduledPublishing.Utils;
using ScheduledPublishing.Validation;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;

namespace ScheduledPublishing.sitecore.shell.Applications.Content_Manager.Dialogs.Edit_Scheduled_Publish
{
    public class EditScheduledPublishDialog : DialogForm
    {
        protected Scrollbox AllSchedules;
        protected Sitecore.Web.UI.HtmlControls.Literal ServerTime;

        private const string CURREN_TIME_ON_SERVER_TEXT = "Current time on server: ";
        private readonly Database _database = Context.ContentDatabase;
        private readonly CultureInfo _culture = Context.Culture;

        protected override void OnLoad(EventArgs e)
        {
            if (!Context.ClientPage.IsEvent)
            {
                ServerTime.Text = CURREN_TIME_ON_SERVER_TEXT + DateTime.Now.ToString(_culture);
                RenderAllSchedules();
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Displays all current publishing schedules ordered by date and time
        /// </summary>
        private void RenderAllSchedules()
        {
            StringBuilder sbHeader = new StringBuilder();
            sbHeader.Append("<table width=\"100%\" cellpadding=\"4\" cellspacing=\"0\">");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<col />");
            sbHeader.Append("<tr style=\"background:#e9e9e9\">");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Item" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Action" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Date" + "</b></td>");
            sbHeader.Append("<td nowrap=\"nowrap\"><b>" + "Delete" + "</b></td>");
            sbHeader.Append("</tr>");
            AllSchedules.Controls.Add(new LiteralControl(sbHeader.ToString()));

            IEnumerable<PublishSchedule> allSchedules = ScheduledPublishRepository.AllSchedules;
            foreach (var schedule in allSchedules)
            {
                if (schedule.InnerItem != null)
                {
                    StringBuilder sbItem = new StringBuilder();
                    // Item name and path
                    sbItem.Append("<tr style='background:#cedff2;border-bottom:1px solid #F0F1F2;'>");
                    Item scheduledItem = schedule.ItemToPublish;
                    sbItem.Append("<td><b>" + scheduledItem.DisplayName + "</b><br />" + scheduledItem.Paths.FullPath + "</td>");

                    // Is publishing/unpublishing
                    sbItem.Append("<td style='border-left:1px solid #F0F1F2;'>");
                    string isUnpublishing = schedule.Unpublish ? "Unpublish" : "Publish";
                    sbItem.Append(isUnpublishing);
                    sbItem.Append("</td><td style='border-left:1px solid #F0F1F2;'>");

                    // Current scheudled publish date and time
                    AllSchedules.Controls.Add(new LiteralControl(sbItem.ToString()));
                    DateTime pbDate = schedule.PublishDate;
                    AllSchedules.Controls.Add(new LiteralControl(pbDate.ToString(_culture)));

                    // Pick new date and time
                    DateTimePicker dtPicker = new DateTimePicker();
                    dtPicker.ID = "dt_" + schedule.InnerItem.ID;
                    dtPicker.Width = new Unit(100.0, UnitType.Percentage);
                    dtPicker.Value = DateUtil.ToIsoDate(schedule.PublishDate);
                    AllSchedules.Controls.Add(dtPicker);
                    AllSchedules.Controls.Add(new LiteralControl("</td>"));

                    // Delete schedule
                    AllSchedules.Controls.Add(new LiteralControl("<td style='border-left:1px solid #F0F1F2;'>"));
                    Checkbox deleteCheckbox = new Checkbox();
                    deleteCheckbox.ID = "del_" + schedule.InnerItem.ID;
                    AllSchedules.Controls.Add(deleteCheckbox);

                    AllSchedules.Controls.Add(new LiteralControl("</td></tr>"));
                }
            }

            AllSchedules.Controls.Add(new LiteralControl("</table"));
        }

        /// <summary>
        /// Save the new publishing schedules
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");

            foreach (string key in Context.ClientPage.ClientRequest.Form.Keys)
            {
                if (key != null && key.StartsWith("dt_", StringComparison.InvariantCulture))
                {
                    string id = StringUtil.Mid(key, 3, 38);

                    DateTimePicker dtEditPicker = AllSchedules.FindControl("dt_" + id) as DateTimePicker;

                    Assert.IsNotNull(dtEditPicker, "dtEditPicker");

                    DateTime dateTime = DateUtil.IsoDateToDateTime(dtEditPicker.Value);
                    PublishSchedule publishSchedule = new PublishSchedule(_database.GetItem(new ID(id)));

                    //Scheudled time has changed
                    if (publishSchedule.PublishDate != dateTime)
                    {
                        publishSchedule.PublishDate = dateTime;

                        ValidationResult validationResult = ScheduledPublishValidator.Validate(publishSchedule);
                        if (!validationResult.IsValid)
                        {
                            SheerResponse.Alert(string.Join(Environment.NewLine, validationResult.ValidationErrors));
                            return;
                        }

                        ScheduledPublishRepository.UpdatePublishSchedule(publishSchedule);
                    }
                }
                else if (key != null && key.StartsWith("del_", StringComparison.InvariantCulture))
                {
                    string id = StringUtil.Mid(key, 4, 38);
                    Checkbox deleteCheckbox = AllSchedules.FindControl("del_" + id) as Checkbox;

                    Assert.IsNotNull(deleteCheckbox, "deleteCheckbox");

                    bool doDelete = deleteCheckbox.Checked;
                    if (doDelete)
                    {
                        Item publishOption = _database.GetItem(new ID(id));
                        ScheduledPublishRepository.DeleteItem(publishOption);
                    }
                }
            }

            base.OnOK(sender, args);
        }
    }
}