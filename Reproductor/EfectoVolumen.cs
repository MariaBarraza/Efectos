using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Reproductor
{
    class EfectoVolumen : ISampleProvider
    {
        private float volume;
        public float Volume
        {
            get
            {
                return volume;
            }
            set
            {
                if (value < 0)
                {
                    volume = 0;
                }
                else if (value > 1)
                {
                    volume = 1;
                }
                else
                {
                    volume = value;
                }                    
            }
        }

        private ISampleProvider fuente;

        public EfectoVolumen(ISampleProvider fuente)
        {
            this.fuente = fuente;
            volume = 1;
        }

        public WaveFormat WaveFormat
        {
            get
            {
                return fuente.WaveFormat;
            }
        }

        //en el buffer se tienen que recibir todas las muestras, el count para contarlas y el offset por si hay algun desface
        public int Read(float[] buffer, int offset, int count)
        {
            var read = fuente.Read(buffer, offset, count);
            //se recorren las muestras leidas (estan guardadas en read
            for (int i = 0; i < read; i++)
            {
                //se modifica el buffer en la posicion offset + i porque todavia no se termina de trabajar en todas las muestras, asi que se pueden dejar algunas sin trabajar en caso de que se necesite
                buffer[offset + i] *= volume;
            }
            //Se regresan el numero de muestras que se leyeron
            return read;
        }
    }
}