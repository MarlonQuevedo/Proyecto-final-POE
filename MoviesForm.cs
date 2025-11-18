using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CineApp
{
    public class MoviesForm : Form
    {
        DataGridView dgv;
        Button btnNuevo;
        Button btnEditar;
        Button btnBack;

        public MoviesForm()
        {
            Text = "Gestionar Películas";
            Width = 600; Height = 400; StartPosition = FormStartPosition.CenterParent;
            dgv = new DataGridView { Left = 10, Top = 10, Width = 560, Height = 300, ReadOnly = true, AutoGenerateColumns = false };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PeliculaId", DataPropertyName = "PeliculaId", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Titulo", DataPropertyName = "Titulo", HeaderText = "Título", Width = 350 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", DataPropertyName = "Precio", HeaderText = "Precio", Width = 100 });

            btnNuevo = new Button { Left = 10, Top = 320, Width = 100, Text = "Nuevo" };
            btnEditar = new Button { Left = 120, Top = 320, Width = 100, Text = "Editar" };
            btnBack = new Button { Left = 240, Top = 320, Width = 100, Text = "Atrás" };
            btnNuevo.Click += BtnNuevo_Click;
            btnEditar.Click += BtnEditar_Click;
            btnBack.Click += (s,e) => { DialogResult = DialogResult.Cancel; };

            Controls.Add(dgv); Controls.Add(btnNuevo); Controls.Add(btnEditar); Controls.Add(btnBack);
            Load += MoviesForm_Load;
        }

        void MoviesForm_Load(object s, EventArgs e)
        {
            LoadMovies();
        }

        void LoadMovies()
        {
            var list = new List<dynamic>();
            var hasPrecio = Db.ColumnExists("Pelicula", "Precio");
            using (var c = Db.NewConnection())
            {
                c.Open();
                string sql;
                if (hasPrecio)
                    sql = "SELECT PeliculaId, Titulo, ISNULL(Precio,0) AS Precio FROM Pelicula ORDER BY Titulo";
                else
                    sql = "SELECT PeliculaId, Titulo FROM Pelicula ORDER BY Titulo";

                using (var cmd = new SqlCommand(sql, c))
                {
                    try
                    {
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                if (hasPrecio)
                                    list.Add(new { PeliculaId = rdr.GetInt32(0), Titulo = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1), Precio = rdr.GetDecimal(2) });
                                else
                                    list.Add(new { PeliculaId = rdr.GetInt32(0), Titulo = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1), Precio = 0m });
                            }
                        }
                    }
                    catch (SqlException ex) when (ex.Number == 207)
                    {
                        // Column not found - fallback to safe query without Precio
                        list.Clear();
                        using (var alt = new SqlCommand("SELECT PeliculaId, Titulo FROM Pelicula ORDER BY Titulo", c))
                        using (var rdr = alt.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                list.Add(new { PeliculaId = rdr.GetInt32(0), Titulo = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1), Precio = 0m });
                            }
                        }
                    }
                }
            }
            dgv.DataSource = list;
        }

        void BtnNuevo_Click(object s, EventArgs e)
        {
            using (var f = new MovieEditForm()) { if (f.ShowDialog(this) == DialogResult.OK) LoadMovies(); }
        }

        void BtnEditar_Click(object s, EventArgs e)
        {
            if (dgv.CurrentRow == null) return;
            var id = (int)dgv.CurrentRow.Cells["PeliculaId"].Value;
            using (var f = new MovieEditForm(id)) { if (f.ShowDialog(this) == DialogResult.OK) LoadMovies(); }
        }
    }

    class MovieEditForm : Form
    {
        TextBox txtTitulo;
        NumericUpDown nudPrecio;
        Label lblPrecio;
        Button btnOk;
        Button btnCancel;
        int? id;

        public MovieEditForm(int? id = null)
        {
            this.id = id;
            Text = id == null ? "Nueva Película" : "Editar Película";
            Width = 400; Height = 180; StartPosition = FormStartPosition.CenterParent;
            Label l1 = new Label { Left = 10, Top = 10, Text = "Título", Width = 50 };
            txtTitulo = new TextBox { Left = 70, Top = 10, Width = 300 };
            lblPrecio = new Label { Left = 10, Top = 45, Text = "Precio", Width = 50 };
            nudPrecio = new NumericUpDown { Left = 70, Top = 45, Width = 120, DecimalPlaces = 2, Maximum = 10000 };
            btnOk = new Button { Left = 70, Top = 80, Width = 100, Text = "OK" };
            btnCancel = new Button { Left = 180, Top = 80, Width = 100, Text = "Cancelar" };
            btnOk.Click += BtnOk_Click; btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.AddRange(new Control[] { l1, txtTitulo, lblPrecio, nudPrecio, btnOk, btnCancel });
            Load += MovieEditForm_Load;
        }

        void MovieEditForm_Load(object s, EventArgs e)
        {
            if (id.HasValue)
            {
                using (var c = Db.NewConnection())
                {
                    c.Open();
                    var hasPrecio = Db.ColumnExists("Pelicula", "Precio");
                    using (var cmd = new SqlCommand(hasPrecio ? "SELECT Titulo, ISNULL(Precio,0) FROM Pelicula WHERE PeliculaId=@Id" : "SELECT Titulo FROM Pelicula WHERE PeliculaId=@Id", c))
                    {
                        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id.Value });
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                txtTitulo.Text = rdr.IsDBNull(0) ? string.Empty : rdr.GetString(0);
                                if (hasPrecio) nudPrecio.Value = rdr.GetDecimal(1);
                                // hide price controls when column not present
                                lblPrecio.Visible = hasPrecio;
                                nudPrecio.Visible = hasPrecio;
                            }
                        }
                    }
                }
            }
            else
            {
                // If creating new and Precio doesn't exist, hide the price controls
                var hasPrecio = Db.ColumnExists("Pelicula", "Precio");
                lblPrecio.Visible = hasPrecio;
                nudPrecio.Visible = hasPrecio;
            }
        }

        void BtnOk_Click(object s, EventArgs e)
        {
            using (var c = Db.NewConnection())
            {
                c.Open();
                var hasPrecio = Db.ColumnExists("Pelicula", "Precio");
                if (id.HasValue)
                {
                    try
                    {
                        if (hasPrecio)
                        {
                            using (var cmd = new SqlCommand("UPDATE Pelicula SET Titulo=@T, Precio=@P WHERE PeliculaId=@Id", c))
                            {
                                cmd.Parameters.Add(new SqlParameter("@T", SqlDbType.NVarChar, 200) { Value = txtTitulo.Text });
                                cmd.Parameters.Add(new SqlParameter("@P", SqlDbType.Decimal) { Value = nudPrecio.Value });
                                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id.Value });
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var cmd = new SqlCommand("UPDATE Pelicula SET Titulo=@T WHERE PeliculaId=@Id", c))
                            {
                                cmd.Parameters.Add(new SqlParameter("@T", SqlDbType.NVarChar, 200) { Value = txtTitulo.Text });
                                cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id.Value });
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (SqlException ex) when (ex.Number == 207)
                    {
                        // invalid column - try fallback without Precio
                        using (var cmd = new SqlCommand("UPDATE Pelicula SET Titulo=@T WHERE PeliculaId=@Id", c))
                        {
                            cmd.Parameters.Add(new SqlParameter("@T", SqlDbType.NVarChar, 200) { Value = txtTitulo.Text });
                            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id.Value });
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    try
                    {
                        if (hasPrecio)
                        {
                            using (var cmd = new SqlCommand("INSERT INTO Pelicula(Titulo,Precio) VALUES(@T,@P);", c))
                            {
                                cmd.Parameters.Add(new SqlParameter("@T", SqlDbType.NVarChar, 200) { Value = txtTitulo.Text });
                                cmd.Parameters.Add(new SqlParameter("@P", SqlDbType.Decimal) { Value = nudPrecio.Value });
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var cmd = new SqlCommand("INSERT INTO Pelicula(Titulo) VALUES(@T);", c))
                            {
                                cmd.Parameters.Add(new SqlParameter("@T", SqlDbType.NVarChar, 200) { Value = txtTitulo.Text });
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (SqlException ex) when (ex.Number == 207)
                    {
                        // fallback: try without Precio
                        using (var cmd = new SqlCommand("INSERT INTO Pelicula(Titulo) VALUES(@T);", c))
                        {
                            cmd.Parameters.Add(new SqlParameter("@T", SqlDbType.NVarChar, 200) { Value = txtTitulo.Text });
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            DialogResult = DialogResult.OK;
        }
    }
}
