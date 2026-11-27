using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Gili
{
    partial class IslandForm
    {
        bool timeZoneDialogOpen;
        string locationStatus = "";
        bool locationFailed;
        TimeZoneInfo clockZone;

        bool PrayerAlarmEnabled { get { return settings.GetBool("prayer.alarm", settings.GetBool("prayer.notify", true)); } }
        TimeZoneInfo ClockZone { get { return clockZone ?? (clockZone = PrayerAlarm.ResolveZone(settings.Get("clock.zone", ""))); } }
        DateTime ClockNow { get { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClockZone); } }

        void OnClockChanged(object sender, EventArgs e)
        {
            Post(delegate { TimeZoneInfo.ClearCachedData(); clockZone = null; Kick(); });
        }

        void SetClockZone(string id)
        {
            clockZone = null;
            SetSetting("clock.zone", id);
        }

        void CheckPrayerAlarm()
        {
            if (!PrayerAlarmEnabled) return;
            string day;
            if (!PrayerAlarm.IsDue(ClockNow, settings.Get("prayer.lastDay", ""), out day)) return;
            var t = ShowToast(View.PrayerAlarm, 30, 3, true);
            if (t == null) return;
            t.Title = "Dale gracias a Jesús";
            t.Detail = "3:00 p. m.";
            // Persist the day so restarting the app during this minute does not ring twice.
            settings.Set("prayer.lastDay", day);
            settings.Save();
            Chime.PrayerAlarm();
            Pulse(Theme.Prayer, 4);
        }

        void DrawTimePage(float w, float h)
        {
            var now = ClockNow;
            var zone = ClockZone;
            paint.Text("Hora y ubicación", Theme.Semi(20), Theme.Text, 20, 62);
            paint.Text(Hm(now), Theme.Semi(30), Theme.Text, 20, 104);
            paint.Text(DateText(), Theme.Ui(12), Theme.Sub, 20, 134);
            float bw = (w - 48) / 2;
            bool system = settings.Get("clock.zone", "").Length == 0;
            PillButton(new RectangleF(20, 154, bw, 32), "clockWindows", "Usar hora de Windows",
                       system ? Theme.WithAlpha(Theme.Prayer, 0.2f) : Theme.Fill, system ? Theme.Prayer : Theme.Text,
                       delegate { SetClockZone(""); });
            PillButton(new RectangleF(28 + bw, 154, bw, 32), "clockChoose", "Elegir zona horaria", Theme.Fill, Theme.Text, OpenTimeZoneDialog);
            paint.Text((system ? "Windows · " : "Zona elegida · ") + zone.DisplayName,
                       Theme.Ui(11), Theme.Sub, 20, 206, w - 40, StringAlignment.Near);

            string city = settings.Get("city", "");
            paint.Icon(Theme.IconPin, 14, Theme.Sub, 28, 244);
            paint.Text(city.Length > 0 ? city : "Ubicación sin elegir", Theme.Semi(12), Theme.Text, 44, 244, w - 266, StringAlignment.Near);
            PillButton(new RectangleF(w - 216, 228, 196, 32), "locate", locating ? "Detectando…" : "Usar mi ubicación",
                       Theme.Fill, locating ? Theme.Sub : Theme.Text, delegate { DetectLocation(false); });
            paint.Text(locationStatus.Length > 0 ? locationStatus : "Ubicación aproximada por internet, solo al pulsar.",
                       Theme.Ui(11), locationFailed ? Theme.Red : Theme.Sub, 20, 280, w - 40, StringAlignment.Near);

            paint.FillRound(new RectangleF(20, 300, w - 40, 64), 16, Theme.Fill);
            Toggle("tgPrayerAlarm", 28, 332, "Gracias a Jesús · 15:00", PrayerAlarmEnabled,
                   delegate { SetSetting("prayer.alarm", !PrayerAlarmEnabled); });
            var next = PrayerAlarm.Next(now);
            paint.Text(PrayerAlarmEnabled ? "Diaria · " + (next.Date == now.Date ? "hoy" : "mañana") + " 15:00" : "Alarma desactivada", Theme.Semi(12), Theme.Text, 270, 322, w - 290, StringAlignment.Near);
            paint.Text("Usa la zona horaria elegida", Theme.Ui(11), Theme.Sub, 270, 344, w - 290, StringAlignment.Near);
        }

        void OpenTimeZoneDialog()
        {
            if (timeZoneDialogOpen) return;
            timeZoneDialogOpen = true;
            expanded = false;
            Kick();
            BeginInvoke(new Action(delegate
            {
                try
                {
                    using (var dialog = new TimeZoneDialog(ClockZone.Id, SetClockZone)) dialog.ShowDialog();
                }
                catch (Exception ex) { Log.Write("time zone dialog: " + ex.Message); }
                finally { timeZoneDialogOpen = false; Expand(2); }
            }));
        }
    }

    /// <summary>Search the time zones installed in Windows; choosing an item applies it immediately.</summary>
    sealed class TimeZoneDialog : Form
    {
        public TimeZoneDialog(string selected, Action<string> pick)
        {
            Text = "Elegir zona horaria · MANAURE";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(560, 172);
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(24, 24, 28);
            ForeColor = Color.White;
            Controls.Add(new Label { Text = "Busca tu ciudad o diferencia UTC", AutoSize = true, Location = new Point(20, 20) });
            var search = new TextBox { Bounds = new Rectangle(20, 48, 520, 28), AccessibleName = "Buscar zona horaria" };
            var zones = new ComboBox { Bounds = new Rectangle(20, 88, 520, 28), DropDownStyle = ComboBoxStyle.DropDownList,
                                       DisplayMember = "DisplayName", AccessibleName = "Zona horaria" };
            var available = TimeZoneInfo.GetSystemTimeZones();
            var status = new Label { AutoSize = true, Location = new Point(20, 136) };
            Action filter = delegate
            {
                zones.Items.Clear();
                foreach (var zone in available)
                {
                    if (CultureInfo.CurrentCulture.CompareInfo.IndexOf(zone.DisplayName, search.Text.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) < 0) continue;
                    zones.Items.Add(zone);
                    if (zone.Id == selected) zones.SelectedItem = zone;
                }
                status.Text = zones.Items.Count == 0 ? "No hay coincidencias. Prueba otra ciudad o diferencia UTC."
                                                    : "Selecciona una zona para aplicarla. Cierra para cancelar.";
            };
            search.TextChanged += delegate { filter(); };
            zones.SelectionChangeCommitted += delegate
            {
                var zone = zones.SelectedItem as TimeZoneInfo;
                if (zone != null) { pick(zone.Id); Close(); }
            };
            Controls.Add(search);
            Controls.Add(zones);
            Controls.Add(status);
            filter();
        }
    }
}
