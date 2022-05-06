using System;
using System.IO;
using System.Xml;

using System.Collections.Generic;

namespace Vlasov_v2_1d
{
    internal class XmlWriter
    {
        private XmlDocument solver;
        private XmlDocument plasma;

        private string solver_path;
        private string plasma_path;

        public XmlWriter(string solver, string plasma)
        {
            this.solver = new XmlDocument();
            this.plasma = new XmlDocument();

            if (File.Exists(solver))
                this.solver.Load(solver);
            else
                CreateNewSolverXml(solver);

            if (File.Exists(plasma))
                this.plasma.Load(plasma);
            else
                CreateNewPlasmaXml(plasma);

            solver_path = solver;
            plasma_path = plasma;
        }

        private void CreateNewSolverXml(string solver_path)
        {
            solver.AppendChild(solver.CreateElement("Solver"));

            //Generating xml node of grid parameters
            XmlElement grid = solver.CreateElement("Grid");

            foreach (string item in Constants.gridVarNames)
            {
                grid.AppendChild(solver.CreateElement(item));
            }
            solver.DocumentElement.AppendChild(grid);
            //=======================================

            //Generating xml node of temporal parameters
            XmlElement temporal = solver.CreateElement("Temporal");

            temporal.AppendChild(solver.CreateElement("tstep"));
            temporal.AppendChild(solver.CreateElement("ntsteps"));

            solver.DocumentElement.AppendChild(temporal);
            //=======================================

            //Generating xml node of boundary configurations
            XmlElement boundary = solver.CreateElement("Boundary");

            boundary.AppendChild(solver.CreateElement("type"));

            XmlElement temp;

            foreach (string item in Constants.boundaryVarNames)
            {
                temp = solver.CreateElement(item);
                temp.SetAttribute("name", "Dirichlet");
                boundary.AppendChild(temp);
            }

            solver.DocumentElement.AppendChild(boundary);
            //======================================

            //Generating xml node of extra configurations
            XmlElement config = solver.CreateElement("Config");

            //Generating xml node of filtration
            temp = solver.CreateElement("filtration");
            temp.SetAttribute("state", "disable");

            temp.AppendChild(solver.CreateElement("period"));
            temp.AppendChild(solver.CreateElement("order"));
            temp.AppendChild(solver.CreateElement("width"));

            config.AppendChild(temp);
            //--------------------------------------

            //Generating xml node of external field
            temp = solver.CreateElement("external");
            temp.SetAttribute("state", "disable");

            temp.AppendChild(solver.CreateElement("magnetic"));
            temp.AppendChild(solver.CreateElement("electric"));

            config.AppendChild(temp);
            //--------------------------------------

            //Generating xml node of diagnostics
            temp = solver.CreateElement("diagnostics");
            temp.AppendChild(solver.CreateElement("path"));

            config.AppendChild(temp);
            //--------------------------------------

            solver.DocumentElement.AppendChild(config);
            //======================================

            solver.Save(solver_path);
        }

        private void CreateNewPlasmaXml(string plasma_path)
        {
            plasma.AppendChild(plasma.CreateElement("Plasma"));

            plasma.Save(plasma_path);
        }

        public void SaveAs(string path, string type)
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
                switch (type)
                {
                    case "plasma":
                        plasma.Save(path);
                        break;
                    case "solver":
                        solver.Save(path);
                        break;
                    default:                 
                        break;
                }
        }

        public void WriteGridAndTemporal(Grid grid)
        {
            XmlElement root = solver.DocumentElement;

            if (grid.isGPU)
                root.SetAttribute("device", "GPU");
            else
                root.SetAttribute("device", "CPU");

            root["Grid"]["xmax"].InnerText = grid.xlength;
            root["Grid"]["xngrids"].InnerText = grid.xngrid;

            root["Temporal"]["tstep"].InnerText = grid.tstep;
            root["Temporal"]["ntsteps"].InnerText = grid.ntsteps;

            foreach (XmlNode item in solver.
                SelectNodes("/Solver/Grid/velGrid"))
            {
                root["Grid"].RemoveChild(item);
            }

            foreach (VelGrid item in grid.velGrids)
            {
                XmlElement velGrid = solver.CreateElement("velGrid");

                velGrid.AppendChild(solver.CreateElement("vmin"));
                velGrid.AppendChild(solver.CreateElement("vmax"));
                velGrid.AppendChild(solver.CreateElement("vngrids"));

                velGrid["vmin"].InnerText = item.vmin;
                velGrid["vmax"].InnerText = item.vmax;
                velGrid["vngrids"].InnerText = item.vngrid;

                velGrid.SetAttribute("name", item.Name);

                root["Grid"].AppendChild(velGrid);
            }

            solver.Save(solver_path);
        }

        public void WriteBoundaryConditions(Boundary boundary)
        {
            XmlElement root = solver.DocumentElement;

            if (boundary.periodic)
                switch (boundary.interpolator)
                {
                    case true:
                        root["Boundary"]["type"].InnerText = "PCHIP_periodic";
                        break;
                    case false:
                        root["Boundary"]["type"].InnerText = "PCSIP_periodic";
                        break;
                }
            else
                switch (boundary.interpolator)
                {
                    case true:
                        root["Boundary"]["type"].InnerText = "PCHIP_natural";
                        break;
                    case false:
                        root["Boundary"]["type"].InnerText = "PCSIP_natural";
                        break;
                }

            int index = 0;

            foreach (bool type in boundary.boundary_types)
            {
                if (type)
                    root["Boundary"][Constants.boundaryVarNames[index]].SetAttribute("name", "Dirichlet");
                else
                    root["Boundary"][Constants.boundaryVarNames[index]].SetAttribute("name", "Neumann");

                root["Boundary"][Constants.boundaryVarNames[index]].InnerText = boundary.boundary_values[index];
                index++;
            }

            solver.Save(solver_path);
        }

        public void WriteExtraConfigs(ExtraConfigs extraConfigs)
        {
            XmlElement root = solver.DocumentElement;

            if (extraConfigs.filtration.enable)
                root["Config"]["filtration"].SetAttribute("state", "enable");
            else
                root["Config"]["filtration"].SetAttribute("state", "disable");

            root["Config"]["filtration"]["period"].InnerText = extraConfigs.filtration.period;
            root["Config"]["filtration"]["order"].InnerText = extraConfigs.filtration.order;
            root["Config"]["filtration"]["width"].InnerText = extraConfigs.filtration.width;

            //Writing external field settings
            if (extraConfigs.external.enable)
                root["Config"]["external"].SetAttribute("state", "enable");
            else
                root["Config"]["external"].SetAttribute("state", "disable");


            foreach (XmlNode item in solver.
                SelectNodes("/Solver/Config/external/field"))
            {
                root["Config"]["external"].RemoveChild(item);
            }

            foreach (Field item in extraConfigs.external.fields)
            {
                XmlElement field = solver.CreateElement("field");

                field.AppendChild(solver.CreateElement("magnetic"));
                field.AppendChild(solver.CreateElement("electric"));

                field["magnetic"].InnerText = item.magnetic;
                field["electric"].InnerText = item.electric;

                field.SetAttribute("type", item.name);

                root["Config"]["external"].AppendChild(field);
            }
            //---------------------------------

            //Writing diagnostics parameters
            if (extraConfigs.diagnostics.enable)
                root["Config"]["diagnostics"].SetAttribute("state", "enable");
            else
                root["Config"]["diagnostics"].SetAttribute("state", "disable");

            root["Config"]["diagnostics"]["path"].InnerText = extraConfigs.diagnostics.path;

            foreach (XmlNode item in solver.
                SelectNodes("/Solver/Config/diagnostics/var"))
            {
                root["Config"]["diagnostics"].RemoveChild(item);
            }

            foreach (DiagVar item in extraConfigs.diagnostics.diagVars)
            {
                XmlElement var = solver.CreateElement("var");

                var.AppendChild(solver.CreateElement("name"));
                var.AppendChild(solver.CreateElement("rate"));
                var["name"].InnerText = item.name;
                var["rate"].InnerText = item.rate;

                root["Config"]["diagnostics"].AppendChild(var);
            }
            //--------------------------------

            solver.Save(solver_path);
        }

        public void WritePlasmaSpecies(List<Particle> particles)
        {
            plasma.RemoveAll();
            plasma.AppendChild(plasma.CreateElement("Plasma"));

            XmlElement root = plasma.DocumentElement;

            foreach (Particle particle in particles)
            {
                XmlElement pnode = plasma.CreateElement("Specie");
                XmlElement temp;

                pnode.SetAttribute("name", particle.Name);
                pnode.SetAttribute("save", particle.isCollectDistr.ToString().ToLower());

                //Writing charge, charge-to-mass ratio and density coefficient of particle
                temp = plasma.CreateElement("charge");
                temp.InnerText = particle.Charge;
                pnode.AppendChild(temp);

                temp = plasma.CreateElement("cmratio");
                temp.InnerText = particle.Cmratio;
                pnode.AppendChild(temp);

                temp = plasma.CreateElement("density");
                temp.InnerText = particle.Density;
                pnode.AppendChild(temp);
                //---------------------------------------------------

                //Writing distribution in configuration space
                temp = plasma.CreateElement("xdistr");
                temp.SetAttribute("number", Convert.ToString(particle.GetXdistr().Count));

                foreach (string xdistr in particle.GetXdistr())
                {
                    XmlElement distr = plasma.CreateElement("distr");
                    distr.InnerText = xdistr;

                    temp.AppendChild(distr);
                }
                pnode.AppendChild(temp);
                //-------------------------------------------

                //Writing distribution in velocity space
                temp = plasma.CreateElement("vdistr");
                temp.SetAttribute("number", Convert.ToString(particle.GetVdistr().Count));

                foreach (string vdistr in particle.GetVdistr())
                {
                    XmlElement distr = plasma.CreateElement("distr");
                    distr.InnerText = vdistr;

                    temp.AppendChild(distr);
                }
                pnode.AppendChild(temp);
                //---------------------------------------

                root.AppendChild(pnode);
            }

            plasma.Save(plasma_path);
        }
    }

    internal class XmlReader
    {
        private string solver_path;
        private string plasma_path;

        public XmlReader(string solver_path, string plasma_path)
        {
            this.solver_path = solver_path;
            this.plasma_path = plasma_path;
        }

        public void ReadSolver(ref Grid grid,
                               ref Boundary boundary, ref ExtraConfigs extraConfigs)
        {
            XmlDocument solver = new XmlDocument();

            if (File.Exists(solver_path))
            {
                solver.Load(solver_path);
                XmlElement root = solver.DocumentElement;

                grid = new Grid(root["Grid"]["xmax"].InnerText,
                                root["Grid"]["xngrids"].InnerText,
                                root["Temporal"]["tstep"].InnerText,
                                root["Temporal"]["ntsteps"].InnerText);

                if (root.GetAttribute("device") == "GPU")
                {
                    grid.isGPU = true;
                }

                foreach (XmlElement item in solver.
                    SelectNodes("/Solver/Grid/velGrid"))
                {
                    grid.AddVelGrids(item["vmin"].InnerText,
                                     item["vmax"].InnerText,
                                     item["vngrids"].InnerText, item.GetAttribute("name"));
                }

                bool periodic = false, interpolator = true;

                if (root["Boundary"]["type"].InnerText.Contains("periodic"))
                    periodic = true;

                if (root["Boundary"]["type"].InnerText.Contains("PCSIP"))
                    interpolator = false;


                boundary = new Boundary(periodic, interpolator);

                bool left_type = true, right_type = true;

                if (root["Boundary"]["left"].GetAttribute("name") == "Neumann")
                    left_type = false;

                if (root["Boundary"]["right"].GetAttribute("name") == "Neumann")
                    right_type = false;

                boundary.SetBoundaryType(left_type, right_type);

                boundary.SetBoundaryValues(root["Boundary"]["left"].InnerText,
                    root["Boundary"]["right"].InnerText);

                XmlElement temp = root["Config"]["filtration"];

                bool fil_enabled = false;
                if (temp.GetAttribute("state") == "enable")
                    fil_enabled = true;

                Filtration filtration = new Filtration(temp["period"].InnerText,
                                                       temp["order"].InnerText,
                                                       temp["width"].InnerText,
                                                       fil_enabled);
                temp = root["Config"]["external"];

                bool ext_enabled = false;
                if (temp.GetAttribute("state") == "enable")
                    ext_enabled = true;

                ExternalField externalField = new ExternalField(ext_enabled);
                foreach (XmlElement item in solver.
                    SelectNodes("/Solver/Config/external/field"))
                    externalField.SetField(item["electric"].InnerText, item["magnetic"].InnerText,
                        item.GetAttribute("type"));

                temp = root["Config"]["diagnostics"];
                bool diag_enabled = false;
                if (temp.GetAttribute("state") == "enable")
                    diag_enabled = true;

                Diagnostics diagnostics = new Diagnostics(temp["path"].InnerText, diag_enabled);

                diagnostics.SetDiagVariables(solver.
                    SelectNodes("/Solver/Config/diagnostics/var"));

                extraConfigs = new ExtraConfigs(filtration, diagnostics, externalField);
            }
        }

        public void ReadPlasma(ref List<Particle> particles)
        {
            XmlDocument plasma = new XmlDocument();

            particles = new List<Particle>();

            if (File.Exists(plasma_path))
            {
                plasma.Load(plasma_path);

                XmlElement root = plasma.DocumentElement;

                foreach (XmlElement item in root.ChildNodes)
                {
                    string name = item.GetAttribute("name");

                    Particle particle = new Particle(name, item["charge"].InnerText,
                                                           item["cmratio"].InnerText,
                                                           item["density"].InnerText);

                    string isSave = item.GetAttribute("save");
                    if (isSave == "true")
                        particle.isCollectDistr = true;

                    particle.SetXdistr(item["xdistr"].ChildNodes);
                    particle.SetVdistr(item["vdistr"].ChildNodes);

                    particles.Add(particle);
                }
            }
        }
    }
}