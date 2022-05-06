using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vlasov_v2_1d
{
    public partial class Form1 : Form
    {
        internal delegate void OnFormChanged<T>(ref T input);

        internal event OnFormChanged<GridBoundaryConfigs> OnGridFormChangedEvent;
        internal event OnFormChanged<ExtraConfigs> OnExtraFormChangedEvent;
        internal event OnFormChanged<List<Particle>> OnPlasmaFormChangedEvent;

        private GridBoundaryConfigs gridb;
        private Grid grid;
        private Boundary boundary;
        private ExtraConfigs extraConfigs;
        private List<Particle> particles;

        private GridBoundary gridBoundaryForm;
        private ExtrasConfig extrasConfigForm;
        private PlasmaConfig plasmaConfigForm;

        private XmlWriter writer;
        private XmlReader reader;

        private List<Form> formList;

        private int current;

        private readonly string[] formNameList =
            { "Grid and Boundary", "Extra configurations", "Plasmas" };


        private string default_path = @"C:\Programmer";
        private readonly string plasma_name = "Plasma.xml";
        private readonly string solver_name = "Solver.xml";

        public Form1()
        {
            InitializeComponent();

            gridBoundaryForm = new GridBoundary(this);
            extrasConfigForm = new ExtrasConfig(this);
            plasmaConfigForm = new PlasmaConfig(this);

            formList = new List<Form>();

            formList.Add(gridBoundaryForm);
            formList.Add(extrasConfigForm);
            formList.Add(plasmaConfigForm);

            grid = new Grid("1", "1", "1", "1");
            grid.AddVelGrids("1", "1", "1", "1");
            boundary = new Boundary(true, true);
            boundary.SetBoundaryType(true, true);
            boundary.SetBoundaryValues("1", "1");

            extraConfigs = new ExtraConfigs(new Filtration("1", "1", "1", false),
                new Diagnostics("", false), new ExternalField(false));

            current = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ChangeForm(formList[0]);

            label1.Text = formNameList[0];

            saveFileDialog1.AddExtension = true;
            saveFileDialog1.Filter = "XML files(*.xml)|*.xml";
            saveFileDialog1.DefaultExt = ".xml";
            saveFileDialog1.Title = "Save as";
            saveFileDialog1.InitialDirectory = @"C:\Programmer";

            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            DialogResult dr = folderBrowserDialog1.ShowDialog();

            if (dr == DialogResult.OK)
            {
                writer = new XmlWriter(Path.Combine(folderBrowserDialog1.SelectedPath, solver_name),
                                       Path.Combine(folderBrowserDialog1.SelectedPath, plasma_name));
                reader = new XmlReader(Path.Combine(folderBrowserDialog1.SelectedPath, solver_name),
                                       Path.Combine(folderBrowserDialog1.SelectedPath, plasma_name));
            }
            else
            {
                writer = new XmlWriter(Path.Combine(default_path, solver_name),
                                       Path.Combine(default_path, plasma_name));
                reader = new XmlReader(Path.Combine(default_path, solver_name),
                                       Path.Combine(default_path, plasma_name));
            }

            try
            {
                reader.ReadPlasma(ref particles);
                reader.ReadSolver(ref grid, ref boundary, ref extraConfigs);
            }
            catch (VlasovInternalException)
            {

            }

            gridBoundaryForm.SetFieldFromXml(grid, boundary);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            switch (current)
            {
                case 0:
                    current++;
                    OnGridFormChangedEvent?.Invoke(ref gridb);
                    grid = gridb.grid; boundary = gridb.boundary;
                    writer.WriteGridAndTemporal(grid);
                    writer.WriteBoundaryConditions(boundary);
                    extrasConfigForm.SetFieldFromXml(extraConfigs);
                    break;
                case 1:
                    current++;
                    OnExtraFormChangedEvent?.Invoke(ref extraConfigs);
                    writer.WriteExtraConfigs(extraConfigs);
                    plasmaConfigForm.SetFieldFromXml(particles);
                    break;
                case 2:
                    current = 0;
                    OnPlasmaFormChangedEvent?.Invoke(ref particles);
                    writer.WritePlasmaSpecies(particles);

                    UpdateVelGridList();
                    UpdateFieldList();

                    gridBoundaryForm.SetFieldFromXml(grid, boundary);
                    break;
            }

            if (current == 0)
                formList[2].Hide();
            else
                formList[current - 1].Hide();
            ChangeForm(formList[current]);
            label1.Text = formNameList[current];

        }

        private void button2_Click(object sender, EventArgs e)
        {
            switch (current)
            {
                case 0:
                    current = 2;
                    OnGridFormChangedEvent.Invoke(ref gridb);
                    grid = gridb.grid;boundary = gridb.boundary;
                    writer.WriteGridAndTemporal(grid);
                    writer.WriteBoundaryConditions(boundary);
                    plasmaConfigForm.SetFieldFromXml(particles);
                    break;
                case 1:
                    current--;
                    OnExtraFormChangedEvent.Invoke(ref extraConfigs);
                    writer.WriteExtraConfigs(extraConfigs);
                    gridBoundaryForm.SetFieldFromXml(grid, boundary);
                    break;
                case 2:
                    current--;
                    OnPlasmaFormChangedEvent.Invoke(ref particles);
                    writer.WritePlasmaSpecies(particles);

                    UpdateVelGridList();
                    UpdateFieldList();

                    extrasConfigForm.SetFieldFromXml(extraConfigs);
                    break;
            }

            if (current == 2)
                formList[0].Hide();
            else
                formList[current + 1].Hide();
            ChangeForm(formList[current]);
            label1.Text = formNameList[current];
        }

        private void UpdateFieldList()
        {
            List<int> rmindexs = new List<int>();
            foreach (Field item in extraConfigs.external.fields)
            {
                bool nonexist = (from particle in particles
                                 where particle.Name == item.name
                                 select true).Count() == 0;
                if (nonexist&&item.name != "global")
                    rmindexs.Add(extraConfigs.external.fields.IndexOf(item));
            }

            foreach (int index in rmindexs)
            {
                extraConfigs.external.fields.RemoveAt(index);
            }

            foreach (Particle item in particles)
            {
                bool nonexist = (from velGrid in extraConfigs.external.fields
                                 where velGrid.name == item.Name
                                 select true).Count() == 0;
                if (nonexist)
                {
                    extraConfigs.external.fields.Add(new Field("@(t, x)0", "@(t, x)0", item.Name));
                }

            }
        }

        private void UpdateVelGridList()
        {
            List<int> rmindexs = new List<int>();
            foreach (VelGrid item in grid.velGrids)
            {
                bool nonexist = (from particle in particles
                                 where particle.Name == item.Name
                                 select true).Count() == 0;
                if (nonexist)
                    rmindexs.Add(grid.velGrids.IndexOf(item));
            }

            foreach (int index in rmindexs)
            {
                grid.velGrids.RemoveAt(index);
            }

            foreach (Particle item in particles)
            {
                bool nonexist = (from velGrid in grid.velGrids
                                 where velGrid.Name == item.Name
                                 select true).Count() == 0;
                if (nonexist)
                {
                    grid.velGrids.Add(new VelGrid("1", "1", "1") { Name = item.Name });
                }
                    
            }
        }

        private void ChangeForm(Form form)
        {
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;

            form.Dock = DockStyle.Fill;
            
            panel1.Controls.Clear();
            panel1.Controls.Add(form);
            form.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult dr = saveFileDialog1.ShowDialog();

            if (dr == DialogResult.OK)
            {
                writer.SaveAs(saveFileDialog1.FileName, "solver");
            }

            dr = saveFileDialog1.ShowDialog();

            if (dr == DialogResult.OK)
            {
                writer.SaveAs(saveFileDialog1.FileName, "plasma");
            }
        }
    }
}
