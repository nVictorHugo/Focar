using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Focar.Models
{
	public class PomodoroRegistroExibicao
	{
		public int Id { get; set; }
		public TipoSessao Tipo { get; set; }
		public DateTime InicioEm { get; set; }
		public DateTime? FimEm { get; set; }
		public int DuracaoMinutos { get; set; }
		public bool Concluida { get; set; }

		public string TipoDescricao => Tipo switch
		{
			TipoSessao.Foco => "Foco",
			TipoSessao.PausaCurta => "Pausa Curta",
			TipoSessao.PausaLonga => "Pausa Longa",
			_ => Tipo.ToString()
		};

		public string TipoIcone => Tipo switch
		{
			TipoSessao.Foco => "Brain",
			TipoSessao.PausaCurta => "CoffeeOutline",
			TipoSessao.PausaLonga => "SleepOutline",
			_ => "HelpCircleOutline"
		};

		public string StatusDescricao => Concluida ? "Concluída" : "Incompleta";

		public string DuracaoFormatada
		{
			get
			{
				if (FimEm is null)
					return $"{DuracaoMinutos} min (planejado)";

				var duracaoReal = FimEm.Value - InicioEm;
				var minutos = Math.Max(0, (int)duracaoReal.TotalMinutes);
				return $"{minutos} min";
			}
		}
	}
}