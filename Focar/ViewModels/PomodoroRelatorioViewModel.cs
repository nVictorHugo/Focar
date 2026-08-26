using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Focar.Data;
using Focar.Models;
using Focar.Data;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Focar.ViewModels
{
	public partial class PomodoroRelatorioViewModel : ObservableObject
	{
		private readonly PomodoroRepositorio _repositorio;

		[ObservableProperty]
		private ObservableCollection<PomodoroRegistroExibicao> registros = new();

		[ObservableProperty]
		private PomodoroRegistroExibicao? registroSelecionado;

		[ObservableProperty]
		private DateTime? dataInicio;

		[ObservableProperty]
		private DateTime? dataFim;

		[ObservableProperty]
		private string tipoFiltro = "Todos";

		[ObservableProperty]
		private bool carregando;

		[ObservableProperty]
		private bool listaVazia;

		[ObservableProperty]
		private string totalRegistrosTexto = "0 registros";

		[ObservableProperty]
		private string tempoFocadoTexto = "0 min";

		public string[] OpcoesTipo { get; } = { "Todos", "Foco", "Pausa Curta", "Pausa Longa" };

		public PomodoroRelatorioViewModel()
		{
			_repositorio = new PomodoroRepositorio();
			_ = CarregarAsync();
		}

		[RelayCommand]
		private async Task Carregar()
		{
			await CarregarAsync();
		}

		[RelayCommand]
		private async Task LimparFiltros()
		{
			dataInicio = null;
			dataFim = null;
			OnPropertyChanged(nameof(DataInicio));
			OnPropertyChanged(nameof(DataFim));
			TipoFiltro = "Todos";
			await CarregarAsync();
		}

		private async Task CarregarAsync()
		{
			Carregando = true;

			try
			{
				TipoSessao? tipo = TipoFiltro switch
				{
					"Foco" => TipoSessao.Foco,
					"Pausa Curta" => TipoSessao.PausaCurta,
					"Pausa Longa" => TipoSessao.PausaLonga,
					_ => null
				};

				var lista = await _repositorio.ListarRegistrosAsync(DataInicio, DataFim, tipo);

				Registros = new ObservableCollection<PomodoroRegistroExibicao>(lista);
				ListaVazia = Registros.Count == 0;

				AtualizarResumo();
			}
			finally
			{
				Carregando = false;
			}
		}

		private void AtualizarResumo()
		{
			TotalRegistrosTexto = Registros.Count == 1
				? "1 registro"
				: $"{Registros.Count} registros";

			var minutosFoco = Registros
				.Where(r => r.Tipo == TipoSessao.Foco && r.Concluida)
				.Sum(r => r.DuracaoMinutos);

			var horas = minutosFoco / 60;
			var minutosRestantes = minutosFoco % 60;

			TempoFocadoTexto = horas > 0
				? $"{horas}h {minutosRestantes:D2}min de foco"
				: $"{minutosRestantes} min de foco";
		}

		[RelayCommand]
		private async Task ExcluirRegistro(PomodoroRegistroExibicao? registro)
		{
			if (registro is null)
				return;

			var confirmar = System.Windows.MessageBox.Show(
				$"Deseja realmente excluir o registro de {registro.TipoDescricao} iniciado em {registro.InicioEm:dd/MM/yyyy HH:mm}?",
				"Confirmar exclusão",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (confirmar != MessageBoxResult.Yes)
				return;

			await _repositorio.ExcluirRegistroAsync(registro.Id);
			Registros.Remove(registro);
			ListaVazia = Registros.Count == 0;
			AtualizarResumo();
		}

		[RelayCommand]
		private async Task ExcluirTodos()
		{
			if (Registros.Count == 0)
				return;

			var confirmar = System.Windows.MessageBox.Show(
				"Deseja realmente excluir TODOS os registros do pomodoro? Essa ação não pode ser desfeita.",
				"Confirmar exclusão total",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (confirmar != MessageBoxResult.Yes)
				return;

			await _repositorio.ExcluirTodosRegistrosAsync();
			Registros.Clear();
			ListaVazia = true;
			AtualizarResumo();
		}

		partial void OnTipoFiltroChanged(string value)
		{
			_ = CarregarAsync();
		}

		partial void OnDataInicioChanged(DateTime? value)
		{
			_ = CarregarAsync();
		}

		partial void OnDataFimChanged(DateTime? value)
		{
			_ = CarregarAsync();
		}
	}
}
