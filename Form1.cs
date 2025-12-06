using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GreenhouseFSM
{
    // BAGIAN 1: CONTROLLER (LOGIKA)
    public class GreenhouseController
    {
        public enum State { SS0_Idle, SS1_Nutri, SS2_Water, SS3_Temp, SS4_Spray, SS5_Flush, SS6_Gas, SS7_Shutdown }

        public bool A1_Injector { get; private set; }
        public bool A2_Pump { get; private set; }
        public bool A3_Fan { get; private set; }
        public bool A4_Sprinkler { get; private set; }
        public bool A5_Valve { get; private set; }
        public bool A6_MCP { get; private set; }

        public State CurrentState { get; private set; }
        private bool q2, q1, q0;

        public GreenhouseController()
        {
            CurrentState = State.SS0_Idle;
        }

        private void CNOT(bool control, ref bool target)
        {
            if (control) target = !target;
        }

        private void VetoGate(bool controlSensor, bool target2, bool target1, bool target0)
        {
            if (controlSensor)
            {
                q2 = target2;
                q1 = target1;
                q0 = target0;
            }
        }

        public void Tick(bool s1, bool s2, bool s3, bool s4, bool s5, bool s6)
        {
            q2 = false; q1 = false; q0 = false;

            // Cascade Priority Logic
            CNOT(s1, ref q0);
            VetoGate(s2, false, true, false);
            VetoGate(s3, false, true, true);
            VetoGate(s4, true, false, false);
            VetoGate(s5, true, false, true);
            VetoGate(s6, true, true, false);

            bool allCritical = s1 && s2 && s3 && s4 && s5 && s6;
            VetoGate(allCritical, true, true, true);

            int stateIndex = (q2 ? 4 : 0) + (q1 ? 2 : 0) + (q0 ? 1 : 0);
            CurrentState = (State)stateIndex;

            UpdateOutputs();
        }

        private void UpdateOutputs()
        {
            A1_Injector = A2_Pump = A3_Fan = A4_Sprinkler = A5_Valve = A6_MCP = false;
            switch (CurrentState)
            {
                case State.SS1_Nutri: A1_Injector = true; break;
                case State.SS2_Water: A2_Pump = true; break;
                case State.SS3_Temp: A3_Fan = true; break;
                case State.SS4_Spray: A4_Sprinkler = true; break;
                case State.SS5_Flush: A5_Valve = true; break;
                case State.SS6_Gas: A6_MCP = true; break;
                case State.SS7_Shutdown: break;
            }
        }
    }

    // BAGIAN 2: FORM / UI 
    public partial class Form1 : Form
    {
        // Komponen UI
        private DataGridView dgvSensors;
        private Label lblCurrentState;
        private Label lblAction;
        private Label lblTotalProb;
        private Button btnNextScenario;
        private GroupBox gbResult;

        // Logic Variables
        private GreenhouseController ctrl;
        private Random rnd;
        private CultureInfo idCulture;
        private double[] basePriorities = { 0.1, 0.3, 0.45, 0.60, 0.75, 1.0 };

        struct SensorData
        {
            public string Name;
            public double Value;
            public double Probability;
            public string TargetSS;
            public bool IsActive;
            public string Unit;
        }

        public Form1()
        {
            InitializeCustomComponents();
            ctrl = new GreenhouseController();
            rnd = new Random();
            idCulture = CultureInfo.CreateSpecificCulture("id-ID");

            RunSimulation();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Greenhouse Quantum FSM Controller";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            // 1. DataGridView untuk Tabel Sensor
            dgvSensors = new DataGridView();
            dgvSensors.Location = new Point(20, 20);
            dgvSensors.Size = new Size(740, 250);
            dgvSensors.ColumnCount = 5;
            dgvSensors.Columns[0].Name = "SENSOR";
            dgvSensors.Columns[1].Name = "VALUE";
            dgvSensors.Columns[2].Name = "LOGIC";
            dgvSensors.Columns[3].Name = "PROBABILITY";
            dgvSensors.Columns[4].Name = "TARGET SS";

            // Styling Grid
            dgvSensors.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSensors.AllowUserToAddRows = false;
            dgvSensors.RowHeadersVisible = false;
            dgvSensors.ReadOnly = true;
            dgvSensors.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSensors.BackgroundColor = Color.White;

            // 2. GroupBox untuk Hasil
            gbResult = new GroupBox();
            gbResult.Text = "System Status";
            gbResult.Location = new Point(20, 290);
            gbResult.Size = new Size(740, 180);

            lblTotalProb = new Label();
            lblTotalProb.Location = new Point(20, 30);
            lblTotalProb.AutoSize = true;
            lblTotalProb.Text = "TOTAL PROBABILITY: -";

            lblCurrentState = new Label();
            lblCurrentState.Location = new Point(20, 60);
            lblCurrentState.AutoSize = true;
            lblCurrentState.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblCurrentState.Text = "CURRENT STATE: -";
            lblCurrentState.ForeColor = Color.Cyan; // Akan diubah dinamis

            lblAction = new Label();
            lblAction.Location = new Point(20, 100);
            lblAction.AutoSize = true;
            lblAction.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblAction.Text = "ACTION: -";

            gbResult.Controls.Add(lblTotalProb);
            gbResult.Controls.Add(lblCurrentState);
            gbResult.Controls.Add(lblAction);

            // 3. Tombol Next Scenario
            btnNextScenario = new Button();
            btnNextScenario.Text = "Generate Next Scenario";
            btnNextScenario.Location = new Point(20, 490);
            btnNextScenario.Size = new Size(740, 50);
            btnNextScenario.BackColor = Color.Teal;
            btnNextScenario.ForeColor = Color.White;
            btnNextScenario.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnNextScenario.Click += BtnNextScenario_Click;

            // Menambahkan control ke Form
            this.Controls.Add(dgvSensors);
            this.Controls.Add(gbResult);
            this.Controls.Add(btnNextScenario);
        }

        private void BtnNextScenario_Click(object sender, EventArgs e)
        {
            RunSimulation();
        }

        private void RunSimulation()
        {
            // --- 1. SMART SCENARIO GENERATOR ---
            int targetScenario = rnd.Next(0, 8);

            // Default Values (Aman)
            double v1 = 4.0; double v2 = 400; double v3 = 28.0;
            double v4 = 0.8; double v5 = 6.0; double v6 = 5.0;

            // Scenario Logic
            switch (targetScenario)
            {
                case 0: break;
                case 1: v1 = 3.6; break;
                case 2: v2 = 800; break;
                case 3: v3 = 35.0; if (rnd.Next(0, 2) == 1) v2 = 900; break;
                case 4: v4 = 0.2; if (rnd.Next(0, 2) == 1) v3 = 33.0; break;
                case 5: v5 = 2.0; break;
                case 6: v6 = 18.0; v1 = 3.6; v3 = 38.0; break;
                case 7: v1 = 3.6; v2 = 900; v3 = 45.0; v4 = 0.1; v5 = 2.5; v6 = 20.0; break;
            }

            // Tambahkan Noise
            v1 += (rnd.NextDouble() * 0.1) - 0.05;
            v3 += (rnd.NextDouble() * 1.0) - 0.5;

            // --- 2. TENTUKAN LOGIC (ACTIVE/IDLE) ---
            bool[] activeStatus = new bool[6];
            activeStatus[0] = v1 < 3.8;                     // S1
            activeStatus[1] = v2 > 600 && v2 < 1000;        // S2
            activeStatus[2] = v3 > 30.0;                    // S3
            activeStatus[3] = v4 < 0.3;                     // S4
            activeStatus[4] = v5 >= 0.5 && v5 <= 3.9;       // S5
            activeStatus[5] = v6 >= 14.0;                   // S6

            // --- 3. HITUNG BOBOT & NORMALISASI ---
            double[] weights = new double[6];
            double totalWeight = 0;

            for (int i = 0; i < 6; i++)
            {
                double noise = rnd.NextDouble() * 0.1;

                if (activeStatus[i])
                    weights[i] = (basePriorities[i] * 50.0) + noise;
                else
                    weights[i] = (rnd.NextDouble() * 0.5);

                totalWeight += weights[i];
            }

            double[] finalProbs = new double[6];
            double checkSum = 0;
            for (int i = 0; i < 6; i++)
            {
                if (totalWeight == 0) finalProbs[i] = 0;
                else finalProbs[i] = weights[i] / totalWeight;

                checkSum += finalProbs[i];
            }

            // --- 4. DATA PACKAGING ---
            var sensors = new List<SensorData>
            {
                new SensorData { Name = "S1 NPK",   Unit="V",    Value = v1, Probability = finalProbs[0], TargetSS = "SS1_NUTRI", IsActive = activeStatus[0] },
                new SensorData { Name = "S2 Water", Unit="ADC",  Value = v2, Probability = finalProbs[1], TargetSS = "SS2_WATER", IsActive = activeStatus[1] },
                new SensorData { Name = "S3 Temp",  Unit="°C",   Value = v3, Probability = finalProbs[2], TargetSS = "SS3_TEMP",  IsActive = activeStatus[2] },
                new SensorData { Name = "S4 NDVI",  Unit="NDVI", Value = v4, Probability = finalProbs[3], TargetSS = "SS4_SPRAY", IsActive = activeStatus[3] },
                new SensorData { Name = "S5 EC",    Unit="dS/m", Value = v5, Probability = finalProbs[4], TargetSS = "SS5_FLUSH", IsActive = activeStatus[4] },
                new SensorData { Name = "S6 Gas",   Unit="ppm",  Value = v6, Probability = finalProbs[5], TargetSS = "SS6_GAS",   IsActive = activeStatus[5] }
            };

            // Eksekusi Controller
            ctrl.Tick(activeStatus[0], activeStatus[1], activeStatus[2], activeStatus[3], activeStatus[4], activeStatus[5]);

            // --- 5. TAMPILAN KE UI ---
            UpdateUI(sensors, checkSum);
        }

        private void UpdateUI(List<SensorData> sensors, double totalProb)
        {
            // Reset Grid
            dgvSensors.Rows.Clear();

            foreach (var s in sensors)
            {
                string statusLogic = s.IsActive ? "1 (ON)" : "0 (OFF)";
                string valStr = $"{s.Value.ToString("F2", idCulture)} {s.Unit}";

                // Tambahkan baris ke DataGridView
                int rowIndex = dgvSensors.Rows.Add(
                    s.Name,
                    valStr,
                    statusLogic,
                    s.Probability.ToString("F4", idCulture),
                    s.TargetSS
                );

                // Highlight baris ketika Logic ON
                if (s.IsActive)
                {
                    dgvSensors.Rows[rowIndex].DefaultCellStyle.BackColor = Color.MistyRose;
                    dgvSensors.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DarkRed;
                    dgvSensors.Rows[rowIndex].DefaultCellStyle.Font = new Font(dgvSensors.Font, FontStyle.Bold);
                }
            }

           
            lblTotalProb.Text = $"TOTAL PROBABILITY SUM: {totalProb.ToString("F4", idCulture)}";

            
            lblCurrentState.Text = $"CURRENT STATE : {ctrl.CurrentState}";

            // Warna teks state
            if (ctrl.CurrentState == GreenhouseController.State.SS0_Idle)
                lblCurrentState.ForeColor = Color.Green;
            else if (ctrl.CurrentState == GreenhouseController.State.SS7_Shutdown || ctrl.CurrentState == GreenhouseController.State.SS6_Gas)
                lblCurrentState.ForeColor = Color.Red;
            else
                lblCurrentState.ForeColor = Color.Blue;

            string aksi = "Standby (Monitoring)";
            if (ctrl.A1_Injector) aksi = "Menyuntikkan Pupuk (Injector ON)";
            else if (ctrl.A2_Pump) aksi = "Menyalakan Pompa Air (Pump ON)";
            else if (ctrl.A3_Fan) aksi = "Menyalakan Kipas Exhaust (Fan ON)";
            else if (ctrl.A4_Sprinkler) aksi = "Menyalakan Sprinkler (Sprayer ON)";
            else if (ctrl.A5_Valve) aksi = "Membuka Valve Flushing (Valve ON)";
            else if (ctrl.A6_MCP) aksi = "Menyemprot Anti-Etilen (1-MCP ON)";
            else if (ctrl.CurrentState == GreenhouseController.State.SS7_Shutdown)
            {
                aksi = "CRITICAL FAILURE: EMERGENCY SHUTDOWN!!";
            }

            lblAction.Text = $"ACTION: {aksi}";
            if (ctrl.CurrentState == GreenhouseController.State.SS7_Shutdown)
                lblAction.ForeColor = Color.Red;
            else
                lblAction.ForeColor = Color.Black;
        }
    }
}