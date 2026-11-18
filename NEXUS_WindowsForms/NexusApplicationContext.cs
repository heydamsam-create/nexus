using NEXUS_WindowsForms.Models;
using NEXUS_WindowsForms.Services;

namespace NEXUS_WindowsForms
{
    internal sealed class NexusApplicationContext : ApplicationContext
    {
        private readonly MissionHistoryStore _historyStore;
        private readonly Form1 _menu;
        private readonly FormMision _mission;
        private readonly FormInformacion _information;
        private readonly FormRegistros _records;
        private bool _isShuttingDown;

        internal NexusApplicationContext()
        {
            _historyStore = new MissionHistoryStore();
            _menu = new Form1();
            _mission = new FormMision();
            _information = new FormInformacion();
            _records = new FormRegistros(_historyStore);

            MainForm = _menu;

            _menu.MissionRequested += (_, _) => ShowScreen(_mission);
            _menu.InformationRequested += (_, _) => ShowScreen(_information);
            _menu.RecordsRequested += (_, _) => OpenRecords();
            _menu.ExitRequested += (_, _) => _menu.Close();

            _mission.MissionCompleted += SaveMissionResult;
            _mission.BackRequested += (_, _) => ShowScreen(_menu);
            _information.BackRequested += (_, _) => ShowScreen(_menu);
            _records.BackRequested += (_, _) => ShowScreen(_menu);

            _mission.FormClosing += OnSecondaryFormClosing;
            _information.FormClosing += OnSecondaryFormClosing;
            _records.FormClosing += OnSecondaryFormClosing;
            _menu.FormClosing += OnMenuFormClosing;

            _menu.Show();
        }

        private void OpenRecords()
        {
            _records.RefreshHistory();
            ShowScreen(_records);
        }

        private void SaveMissionResult(MissionResult result)
        {
            if (!_historyStore.TrySave(result, out _, out string errorMessage))
            {
                MessageBox.Show(
                    _mission,
                    $"No se pudo guardar el resultado de esta misión.\r\n\r\n{errorMessage}",
                    "NEXUS | No se guardó la misión",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ShowScreen(Form screenToShow)
        {
            if (_isShuttingDown)
            {
                return;
            }

            foreach (Form screen in new Form[] { _menu, _mission, _information, _records })
            {
                if (screen != screenToShow && screen.Visible)
                {
                    screen.Hide();
                }
            }

            if (!screenToShow.Visible)
            {
                screenToShow.Show();
            }

            screenToShow.Activate();
        }

        private void OnSecondaryFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isShuttingDown)
            {
                return;
            }

            // The X button on a secondary screen behaves like its Back button.
            e.Cancel = true;
            ShowScreen(_menu);
        }

        private void OnMenuFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isShuttingDown)
            {
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                _menu,
                "¿Quieres cerrar NEXUS?\r\n\r\nEl progreso de una misión que aún no haya finalizado no se guarda.",
                "NEXUS | Confirmar salida",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _isShuttingDown = true;

            // Close hidden screens too, so no forms remain alive after the menu closes.
            _mission.Close();
            _information.Close();
            _records.Close();
        }
    }
}
