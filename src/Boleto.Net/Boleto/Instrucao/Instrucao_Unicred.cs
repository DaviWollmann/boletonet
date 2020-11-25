﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoletoNet
{
    public enum EnumInstrucoes_Unicred1
    {
        Remessa = 1,
        PedidoBaixa = 2,
        ConcessaoAbatimento = 4,
        CancelamentoAbatimento = 5,
        AlteracaoVencimento = 6,
        AlteracaoSeuNumero = 8,
        Protestar = 9,
        SustaProtestoManterCarteira = 11,
        SustarProtestoBaixarTitulo = 25,
        ProtestoAutomatico = 26,
        AlteracaoOutrosDados = 31, //alteração de dados do pagador
        AlteracaoCarteira = 40
    }
    public class Instrucao_Unicred : AbstractInstrucao, IInstrucao
    {

        private int _NumeroInstrucao;
        public int NumeroInstrucao { get { return this._NumeroInstrucao; } set { this._NumeroInstrucao = value; } }

        #region Construtores

        public Instrucao_Unicred()
        {
            try
            {
                this.Banco = new Banco(136);
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao carregar objeto", e);
            }
        }

        public Instrucao_Unicred(int codigo, int nrDias)
        {
            try
            {
                this.Banco = new Banco_Unicred();
                this.ExecInstrucao(codigo, nrDias);
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao carregar objeto", e);
            }
        }
        #endregion Construtores



        private void ExecInstrucao(int codigo, int nrDias)
        {
            switch ((EnumInstrucoes_Unicred1)codigo)
            {
                case EnumInstrucoes_Unicred1.Remessa:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.Remessa;
                    this.Descricao = "REMESSA";
                    break;
                case EnumInstrucoes_Unicred1.PedidoBaixa:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.PedidoBaixa;
                    this.Descricao = "PEDIDO DE BAIXA";
                    break;
                case EnumInstrucoes_Unicred1.ConcessaoAbatimento:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.ConcessaoAbatimento;
                    this.Descricao = "CONCESSÃO DE ABATIMENTO";
                    break;
                case EnumInstrucoes_Unicred1.CancelamentoAbatimento:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.CancelamentoAbatimento;
                    this.Descricao = "CANCELAMENTO DE ABATIMENTO";
                    break;
                case EnumInstrucoes_Unicred1.AlteracaoVencimento:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.AlteracaoVencimento;
                    this.Descricao = "ALTERAÇÃO DE VENCIMENTO";
                    break;
                case EnumInstrucoes_Unicred1.AlteracaoSeuNumero:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.AlteracaoSeuNumero;
                    this.Descricao = "ALTERAÇÃO DE SEU NÚMERO";
                    break;
                case EnumInstrucoes_Unicred1.Protestar:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.Protestar;
                    this.Descricao = "PROTESTAR";
                    break;
                case EnumInstrucoes_Unicred1.SustaProtestoManterCarteira:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.SustaProtestoManterCarteira;
                    this.Descricao = "SUSTAR PROTESTO E MANTER EM CARTEIRA";
                    break;
                case EnumInstrucoes_Unicred1.SustarProtestoBaixarTitulo:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.SustarProtestoBaixarTitulo;
                    this.Descricao = "SUSTAR PROTESTO E BAIXAR TITULO";
                    break;
                case EnumInstrucoes_Unicred1.ProtestoAutomatico:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.ProtestoAutomatico;
                    this.Descricao = "PROTESTO AUTOMÁTICO";
                    break;
                case EnumInstrucoes_Unicred1.AlteracaoOutrosDados:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.AlteracaoOutrosDados;
                    this.Descricao = "ALTERAÇÃO DE OUTROS DADOS (ALTERAÇÃO DE DADOS DO PAGADOR)";
                    break;
                case EnumInstrucoes_Unicred1.AlteracaoCarteira:
                    this.Codigo = (int)EnumInstrucoes_Unicred1.AlteracaoCarteira;
                    this.Descricao = "ALTERAÇÃO DE CARTEIRA";
                    break;

            }
            this.QuantidadeDias = nrDias;
        }
    }
}
