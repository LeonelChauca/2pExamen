using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace _2Img
{
    public partial class Form1 : Form
    {
        private Dictionary<Color, Color> colorMappings = new Dictionary<Color, Color>();
        private Dictionary<Point, Color> originalPixelColors = new Dictionary<Point, Color>();
        private int tolerance = 50; // Definir un rango de tolerancia para los colores
        private Bitmap originalBitmap;
        private Bitmap modifiedBitmap;

        public Form1()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // Ajustar la imagen al tamaño del PictureBox
            pictureBox1.MouseClick += PictureBox1_MouseClick; // Agregar evento de clic
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            conex();
            LoadColorsFromDatabase();
        }

        // Conexión con la base de datos de MySQL
        private void conex()
        {
            string connetionString = "server=localhost;database=2pfinal;uid=root;pwd=;";
            MySqlConnection cnn = new MySqlConnection(connetionString);
            try
            {
                cnn.Open();
                MessageBox.Show("Connection Open!");
                cnn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can not open connection!");
            }
        }

        // Cargar colores desde la base de datos
        private void LoadColorsFromDatabase()
        {
            string connetionString = "server=localhost;database=2pfinal;uid=root;pwd=;";
            MySqlConnection cnn = new MySqlConnection(connetionString);
            try
            {
                cnn.Open();
                string query = "SELECT ColoPrin, ColorCam FROM imagetextures";
                MySqlCommand cmd = new MySqlCommand(query, cnn);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string originalColorString = reader["ColoPrin"].ToString();
                    string changeColorString = reader["ColorCam"].ToString();
                    Color originalColor = ParseColor(originalColorString);
                    Color changeColor = ParseColor(changeColorString);
                    colorMappings[originalColor] = changeColor;
                }
                reader.Close();
                cnn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading colors from database!");
            }
        }

        // Subir imagen y mostrar en PictureBox sin modificarla
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            if (open.ShowDialog() == DialogResult.OK)
            {
                originalBitmap = new Bitmap(open.FileName);
                pictureBox1.Image = originalBitmap;
                originalPixelColors.Clear(); // Clear previous original colors
            }
        }

        // Aplicar cambios a la imagen basada en la base de datos y mostrar en PictureBox
        private void button2_Click(object sender, EventArgs e)
        {
            if (originalBitmap == null)
            {
                MessageBox.Show("Primero sube una imagen.");
                return;
            }

            modifiedBitmap = ChangeColorsBasedOnDatabase(new Bitmap(originalBitmap));
            pictureBox1.Image = modifiedBitmap;
        }

        // Función para cambiar píxeles basado en colores de la base de datos
        private Bitmap ChangeColorsBasedOnDatabase(Bitmap bitmap)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    foreach (var mapping in colorMappings)
                    {
                        if (IsColorInRange(pixelColor, mapping.Key))
                        {
                            originalPixelColors[new Point(x, y)] = pixelColor;
                            ChangeSurroundingPixels(bitmap, x, y, mapping.Value);
                            break;
                        }
                    }
                }
            }
            return bitmap;
        }

        private bool IsColorInRange(Color color1, Color color2)
        {
            return Math.Abs(color1.R - color2.R) <= tolerance &&
                   Math.Abs(color1.G - color2.G) <= tolerance &&
                   Math.Abs(color1.B - color2.B) <= tolerance;
        }

        private Color ParseColor(string colorString)
        {
            string[] parts = colorString.Split(',');
            if (parts.Length == 3)
            {
                int r = int.Parse(parts[0]);
                int g = int.Parse(parts[1]);
                int b = int.Parse(parts[2]);
                return Color.FromArgb(r, g, b);
            }
            return Color.White;
        }

        private void ChangeSurroundingPixels(Bitmap bitmap, int x, int y, Color newColor)
        {
            int range = 10;

            for (int dy = -range; dy <= range; dy++)
            {
                for (int dx = -range; dx <= range; dx++)
                {
                    int newX = x + dx;
                    int newY = y + dy;

                    if (newX >= 0 && newX < bitmap.Width && newY >= 0 && newY < bitmap.Height)
                    {
                        bitmap.SetPixel(newX, newY, newColor);
                    }
                }
            }
        }

        private void PictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (modifiedBitmap == null) return;

            // Obtener el punto de clic relativo al PictureBox
            Point point = GetImageCoordinates(e.Location);
            if (point.X < 0 || point.Y < 0 || point.X >= modifiedBitmap.Width || point.Y >= modifiedBitmap.Height)
                return;

            Color clickedColor = modifiedBitmap.GetPixel(point.X, point.Y);
            if (originalPixelColors.ContainsKey(point))
            {
                Color originalColor = originalPixelColors[point];
                textBox1.Text = $"Original: {originalColor} (RGB: {originalColor.R}, {originalColor.G}, {originalColor.B})\n" +
                                $"Cambiado: {clickedColor} (RGB: {clickedColor.R}, {clickedColor.G}, {clickedColor.B})";
                colorPanel.BackColor = clickedColor;
            }
            else
            {
                textBox1.Text = $"Cambiado: {clickedColor} (RGB: {clickedColor.R}, {clickedColor.G}, {clickedColor.B})";
                colorPanel.BackColor = clickedColor;
            }
        }

        private Point GetImageCoordinates(Point point)
        {
            int x = point.X * modifiedBitmap.Width / pictureBox1.Width;
            int y = point.Y * modifiedBitmap.Height / pictureBox1.Height;
            return new Point(x, y);
        }
    }
}
