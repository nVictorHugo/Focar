using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Focar.Models
{
	public enum TipoSessao
	{
		Foco,
		PausaCurta,
		PausaLonga
	}

	public class PomodoroSessao
	{
		public int Id { get; set; }
		public TipoSessao Tipo { get; set; }
		public DateTime InicioEm { get; set; }
		public DateTime? FimEm { get; set; }
		public int DuracaoMinutos { get; set; }
		public bool Concluida { get; set; }
	}

}
