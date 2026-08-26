using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;


namespace Focar.Services
{
	public class AudioService
	{
		public static void TocarAlerta()
		{
			var caminho = Path.Combine(AppContext.BaseDirectory, 
			"Resources", 
			"Sounds",
			"discord-notification.mp3");

			var arquivoDeAudio = new AudioFileReader(caminho);
			var dispositivoDeSaida = new WaveOutEvent();

		    dispositivoDeSaida.Init(arquivoDeAudio);

			dispositivoDeSaida.PlaybackStopped += (_, _) =>
			{
				dispositivoDeSaida.Dispose();
				arquivoDeAudio.Dispose();
			};

			dispositivoDeSaida.Play();
		}
	}
}
