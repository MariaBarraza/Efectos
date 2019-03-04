using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using Microsoft.Win32;

using System.Windows.Threading;

namespace Reproductor
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //lee el arvchivo de audio
        AudioFileReader reader;
        //dispositivo de salida, responde a los eventos como pausa, play y eso, se comunica con la tarjeta de sonido
        //nuestra comunicacion con la tarjeta de sonido
        WaveOutEvent output;

        //cuando se trabaja con hilos se usa dispatcher, esto se refiere a un proceso que se va a comunicar de manera segura con la interfaz de usuario
        //un dispatcher ejecuta procesos cada cierta cantidad de tiempo, el tiempo nosotros lo establecemos
        DispatcherTimer timer;

        //Se modifica el volumen del audio y no de la fuente de sonido para no afectar a todo lo que se esta escuchando
        VolumeSampleProvider volume;

        //se declara para el efecto del fade in y dafe out
        FadeInOutSampleProvider fades;

        //es una variable para validar si se esta arrastrando o no el slider
        bool dragging = false;
        //para que solo se pueda usar el fade out una vez
        bool fadingOut = false;


        public MainWindow()
        {
            InitializeComponent();
            LlenarComboSalida();

            //este timer va a ejecutar una funcion, despues va a esperar lo que pongamos aqui y luego lo va a ejecutar y repetira
            //inicializar timer
            timer = new DispatcherTimer();

            //Aqui se  especificar cada que cantidad de tiempo queremos que se ejecute
            //Si queremos que actualice cada segundo es bueno poner medio segundo 
            timer.Interval = TimeSpan.FromMilliseconds(100);
            //aqui se establece que va a hacer
            //el operador += dice que hay que hacer algo con el evento, se da tab antes de dar espacio al += para crear la funcion automaticamente
            timer.Tick += Timer_Tick;

            

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if(reader != null)
            {
                lblTiempoActual.Text = reader.CurrentTime.ToString().Substring(0, 8);
                if(!dragging)
                {
                    sldReproduccion.Value = reader.CurrentTime.TotalSeconds;
                }
                
            }
        }

        private void LlenarComboSalida()
        {
            cbSalida.Items.Clear();
            for(int i=0; i<WaveOut.DeviceCount; i++)
            {
                //da las capacidades disponibles de un dispositivo en especifico
                WaveOutCapabilities capacidades = WaveOut.GetCapabilities(i);
                //se añaden al combobox los nombres 
                cbSalida.Items.Add(capacidades.ProductName);
            }
            cbSalida.SelectedIndex = 0;
        }

        private void btnElegirArchivo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = 
                new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                txtRutaArchivo.Text =
                     openFileDialog.FileName;
            }
        }

        private void btnReproducir_Click(object sender, RoutedEventArgs e)
        {

            //evitar duplicado despues del primer play
            //si ademas de ya tener el output esta pausado
            if (output != null && output.PlaybackState == PlaybackState.Paused)
            {
                output.Play();

                //activamos y desactivamos los botones para poner restricciones en la interfaz
                btnReproducir.IsEnabled = false;
                btnPausa.IsEnabled = true;
                btnDetener.IsEnabled = true;

            }
            else
            {
                reader = new AudioFileReader(txtRutaArchivo.Text);

                //Se le da el archivo y si queremos que inicie en total silencio o no
                fades = new FadeInOutSampleProvider(reader, true);
                //se transforman los segundos a mini segundos para la duracion del fade
                double milisegundosFadeIn = Double.Parse(txtDuracionFadeIn.Text) * 1000.0;
                fades.BeginFadeIn(milisegundosFadeIn);

                //hay que reiniciar cuando se reinicie la cancion
                fadingOut = false;
                output = new WaveOutEvent();

                //aqui se cambia donde se reproduce
                output.DeviceNumber = cbSalida.SelectedIndex;

                //se añaden reacciones a los eventos con += pues asi se sobrecarga el operador
                //con tab crea la funcion visual (output_playbackstopped)
                output.PlaybackStopped += Output_PlaybackStopped;

                volume = new VolumeSampleProvider(fades);
                volume.Volume = (float)sldVolumen.Value;

                //inisializamos el output
                output.Init(volume);
                output.Play();

                //activamos y desactivamos los botones para poner restricciones en la interfaz
                btnDetener.IsEnabled = true;
                btnPausa.IsEnabled = true;
                btnReproducir.IsEnabled = false;

                //se quitan los milisegundos y eso
                lblTiempoFinal.Text = reader.TotalTime.ToString().Substring(0, 8);

                //se le asigna a la barrita el tiempo total que dura la cancion como double
                sldReproduccion.Maximum = reader.TotalTime.TotalSeconds;
                //se le asigna el valor actual a la barrita
                sldReproduccion.Value = reader.CurrentTime.TotalSeconds;
                //asi se inicia el contador
                timer.Start();
            }
            
        }

        private void Output_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            reader.Dispose();
            output.Dispose();
            //Aqui es donde seria mas prudente detener el timer porque lo haria al terminar lo que se guarda en el ultimo buffer
            timer.Stop();
        }

        private void btnPausa_Click(object sender, RoutedEventArgs e)
        {
            if(output != null)
            {
                output.Pause();

                //activamos y desactivamos los botones para poner restricciones en la interfaz
                btnDetener.IsEnabled = true;
                btnPausa.IsEnabled = false;
                btnReproducir.IsEnabled = true;
            }
        }

        private void btnDetener_Click(object sender, RoutedEventArgs e)
        {
            if (output!= null)
            {
                output.Stop();

                //activamos y desactivamos los botones para poner restricciones en la interfaz
                btnDetener.IsEnabled = false;
                btnPausa.IsEnabled = false;
                btnReproducir.IsEnabled = true;
            }
        }
        private void sldReproduccion_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            dragging = false;
            //aqui se le asigna el valor actual del slider al tiempo
            if(reader!=null && output != null && output.PlaybackState != PlaybackState.Stopped)
            {
                reader.CurrentTime = TimeSpan.FromSeconds(sldReproduccion.Value);
            }
        }

        private void sldReproduccion_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            dragging = true;
        }

        private void sldVolumen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(volume!=null && output != null && output.PlaybackState != PlaybackState.Stopped)
            {
                volume.Volume = (float)sldVolumen.Value;
            }
            if(lblPorcentajeVolumen !=  null)
            {
                lblPorcentajeVolumen.Text = ((int)(sldVolumen.Value * 100)).ToString() + " %";
            }
            
        }

        private void btnFadeOut_Click(object sender, RoutedEventArgs e)
        {
            if(!fadingOut && fades!=null && output != null && output.PlaybackState == PlaybackState.Playing)
            {
                fadingOut = true;
                double milisegundosFadeOut = Double.Parse(txtDuracionFadeOut.Text) * 1000;
                fades.BeginFadeOut(milisegundosFadeOut);
            }
        }
    }
}
