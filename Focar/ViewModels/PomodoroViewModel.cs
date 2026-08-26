using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Focar.Data;
using Focar.Data;
using Focar.Helpers;
using Focar.Helpers;
using Focar.Models;
using Focar.Models;
using Focar.Services;
using Focar.Views;
using Focar.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


namespace Focar.ViewModels
{
	public partial class PomodoroViewModel : ObservableObject
	{
		private readonly PomodoroRepositorio _repository;
		private readonly DispatcherTimer _timer;

		private int _focoMinutos = 25;
		private int _pausaCurtaMinutos = 5;
		private int _pausaLongaMinutos = 15;
		private int _ciclosParaPausaLonga = 4;

		private int? _idSessaoAtual;
		private TimeSpan _tempoRestante;
		private TimeSpan _duracaoTotalSessao;
		private int _ciclosCompletos;

		[ObservableProperty]
		private string timeDisplay = "25:00";

		[ObservableProperty]
		private string sessionLabel = "Foco";

		[ObservableProperty]
		private double progress;

		[ObservableProperty]
		private string cicloAtualTexto = "Ciclo 1 de 4";

		[ObservableProperty]
		private string cicloAtual = "#1";

		[ObservableProperty]
		private bool estaRodando;

		[ObservableProperty]
		private bool isStopped = true;

		[ObservableProperty]
		private int pomodorosConcluidos;

		private TipoSessao _sessaoAtual = TipoSessao.Foco;

		public PomodoroViewModel()
		{
			_repository = new PomodoroRepositorio();

			_timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			_timer.Tick += Timer_Tick;

			_ = InicializarAsync();
		}

		private async System.Threading.Tasks.Task InicializarAsync()
		{
			await CarregarConfiguracoesAsync();
			PomodorosConcluidos = await _repository.ContarPomodorosConcluidosHojeAsync();
			ResetarParaNovaSessao(TipoSessao.Foco, primeiraVez: true);
		}

		private async System.Threading.Tasks.Task CarregarConfiguracoesAsync()
		{
			_focoMinutos = await ObterConfigIntAsync("FocoMinutos", _focoMinutos);
			_pausaCurtaMinutos = await ObterConfigIntAsync("PausaCurtaMinutos", _pausaCurtaMinutos);
			_pausaLongaMinutos = await ObterConfigIntAsync("PausaLongaMinutos", _pausaLongaMinutos);
			_ciclosParaPausaLonga = await ObterConfigIntAsync("CiclosParaPausaLonga", _ciclosParaPausaLonga);
		}

		private async System.Threading.Tasks.Task<int> ObterConfigIntAsync(string chave, int padrao)
		{
			var valor = await _repository.ObterConfigAsync(chave);
			return int.TryParse(valor, out var resultado) ? resultado : padrao;
		}


		[RelayCommand]
		private async System.Threading.Tasks.Task StartPause()
		{
			if (EstaRodando)
			{
				Pausar();
			}
			else
			{
				await IniciarAsync();
			}
		}

		[RelayCommand]
		private async System.Threading.Tasks.Task Reset()
		{
			_timer.Stop();
			EstaRodando = false;
			IsStopped = !EstaRodando;

			if (_idSessaoAtual is not null)
			{
				await _repository.FinalizarSessaoAsync(_idSessaoAtual.Value, concluida: false);
				_idSessaoAtual = null;
			}

			ResetarParaNovaSessao(_sessaoAtual, primeiraVez: false);
		}

		[RelayCommand]
		private async System.Threading.Tasks.Task Skip()
		{
			_timer.Stop();
			EstaRodando = false;
			IsStopped = !EstaRodando;

			if (_idSessaoAtual is not null)
			{
				await _repository.FinalizarSessaoAsync(_idSessaoAtual.Value, concluida: false);
				_idSessaoAtual = null;
			}

			await AvancarParaProximaSessaoAsync();
		}

		[RelayCommand]
		private void AbrirConfiguracoes()
		{
			// configuracao do app (vou fazer em breve)
		}

		[RelayCommand]
		private void AbrirRelatorioPomodoro()
		{
			var janela = new PomodoroRelatorioWindow();

			janela.Show();
		}

		private async System.Threading.Tasks.Task IniciarAsync()
		{
			if (_idSessaoAtual is null)
			{
				var sessao = new PomodoroSessao
				{
					Tipo = _sessaoAtual,
					InicioEm = DateTime.Now,
					DuracaoMinutos = (int)_duracaoTotalSessao.TotalMinutes
				};

				_idSessaoAtual = await _repository.IniciarSessaoAsync(sessao);
			}

			EstaRodando = true;
			IsStopped = !EstaRodando;
			_timer.Start();
		}

		private void Pausar()
		{
			_timer.Stop();
			EstaRodando = false;
			IsStopped = !EstaRodando;
		}

		private async void Timer_Tick(object? sender, EventArgs e)
		{
			if (_tempoRestante.TotalSeconds <= 0)
			{
				_timer.Stop();
				EstaRodando = false;
				IsStopped = !EstaRodando;

				if (_idSessaoAtual is not null)
				{
					await _repository.FinalizarSessaoAsync(_idSessaoAtual.Value, concluida: true);

					_idSessaoAtual = null;
				}

				if (_sessaoAtual == TipoSessao.Foco)
				{
					PomodorosConcluidos++;
					_ciclosCompletos++;
				}

				await AvancarParaProximaSessaoAsync();
				return;
			}

			_tempoRestante = _tempoRestante.Subtract(TimeSpan.FromSeconds(1));
			AtualizarDisplay();
		}

		private async System.Threading.Tasks.Task AvancarParaProximaSessaoAsync()
		{
			TipoSessao proxima;

			if (_sessaoAtual == TipoSessao.Foco)
			{
				proxima = (_ciclosCompletos % _ciclosParaPausaLonga == 0)
					? TipoSessao.PausaLonga
					: TipoSessao.PausaCurta;

				if (proxima == TipoSessao.PausaCurta)
				{
					var mensagensDescanso = new PomodoroMensagensDescanso();
					_ = CongelarMouseTecladoPorTempo(3);
				}
			}
			else
			{
				var mensagensTrabalho = new PomodoroMensagensTrabalho();
				proxima = TipoSessao.Foco;
			}

			ResetarParaNovaSessao(proxima, primeiraVez: false);

			await IniciarAsync();
		}


		private void ResetarParaNovaSessao(TipoSessao tipo, bool primeiraVez)
		{
			_sessaoAtual = tipo;

			var minutos = tipo switch
			{
				TipoSessao.Foco => _focoMinutos,
				TipoSessao.PausaCurta => _pausaCurtaMinutos,
				TipoSessao.PausaLonga => _pausaLongaMinutos,
				_ => _focoMinutos
			};

			_duracaoTotalSessao = TimeSpan.FromMinutes(minutos);
			_tempoRestante = _duracaoTotalSessao;

			SessionLabel = tipo switch
			{
				TipoSessao.Foco => "Foco",
				TipoSessao.PausaCurta => "Pausa Curta",
				TipoSessao.PausaLonga => "Pausa Longa",
				_ => "Foco"
			};

			var cicloExibido = (_ciclosCompletos % _ciclosParaPausaLonga) + 1;
			CicloAtualTexto = $"Ciclo {cicloExibido} de {_ciclosParaPausaLonga}";
			CicloAtual = $"#{_ciclosCompletos + 1}";

			AtualizarDisplay();
		}

		private void AtualizarDisplay()
		{
			TimeDisplay = _tempoRestante.ToString(@"mm\:ss");
			Progress = _duracaoTotalSessao.TotalSeconds > 0
				? 100.0 * (1 - (_tempoRestante.TotalSeconds / _duracaoTotalSessao.TotalSeconds))
				: 0;
		}

		async Task CongelarMouseTecladoPorTempo(int segundos)
		{
			AlertaSonoro();
			InputBlocker.Block();
			await Task.Delay(TimeSpan.FromSeconds(segundos));
			InputBlocker.Unblock();
		}

		public static void AlertaSonoro()
		{
			SystemSounds.Exclamation.Play();
		}

		public static class WindowsLock
		{
			[DllImport("user32.dll", SetLastError = true)]
			private static extern bool LockWorkStation();

			public static void Lock()
			{
				LockWorkStation();
			}
		}
		public static class InputBlocker
		{
			[DllImport("user32.dll")]
			private static extern bool BlockInput(bool fBlockIt);

			public static void Block()
			{
				BlockInput(true);
			}

			public static void Unblock()
			{
				BlockInput(false);
			}
		}
	}
}
