using BoletoNet.EDI.Banco;
using BoletoNet.Excecoes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

[assembly: WebResource("BoletoNet.Imagens.748.jpg", "image/jpg")]
namespace BoletoNet
{
    /// <Author>
    /// Samuel Schmidt - Sicredi Nordeste RS / Felipe Eduardo - RS
    /// </Author>
    internal class Banco_Sicredi : AbstractBanco, IBanco
    {
        private static Dictionary<int, string> carteirasDisponiveis = new Dictionary<int, string>() {
            { 1, "Com Registro" },
            { 3, "Sem Registro" }
        };

        private HeaderRetorno header;

        /// <author>
        /// Classe responsavel em criar os campos do Banco Sicredi.
        /// </author>
        internal Banco_Sicredi()
        {
            this.Codigo = 748;
            this.Digito = "X";
            this.Nome = "Banco Sicredi";
        }

        public override void ValidaBoleto(Boleto boleto)
        {
            //Formata o tamanho do n�mero da ag�ncia
            if (boleto.Cedente.ContaBancaria.Agencia.Length < 4)
                boleto.Cedente.ContaBancaria.Agencia = Utils.FormatCode(boleto.Cedente.ContaBancaria.Agencia, 4);

            //Formata o tamanho do n�mero da conta corrente
            if (boleto.Cedente.ContaBancaria.Conta.Length < 5)
                boleto.Cedente.ContaBancaria.Conta = Utils.FormatCode(boleto.Cedente.ContaBancaria.Conta, 5);

            //Atribui o nome do banco ao local de pagamento
            if (boleto.LocalPagamento == "At� o vencimento, preferencialmente no ")
                boleto.LocalPagamento += Nome;
            else boleto.LocalPagamento = "PAG�VEL PREFERENCIALMENTE NAS COOPERATIVAS DE CR�DITO DO SICREDI";

            //Verifica se data do processamento � valida
            if (boleto.DataProcessamento == DateTime.MinValue) // diegomodolo (diego.ribeiro@nectarnet.com.br)
                boleto.DataProcessamento = DateTime.Now;

            //Verifica se data do documento � valida
            if (boleto.DataDocumento == DateTime.MinValue) // diegomodolo (diego.ribeiro@nectarnet.com.br)
                boleto.DataDocumento = DateTime.Now;

            string infoFormatoCodigoCedente = "formato AAAAPPCCCCC, onde: AAAA = N�mero da ag�ncia, PP = Posto do benefici�rio, CCCCC = C�digo do benefici�rio";

            var codigoCedente = Utils.FormatCode(boleto.Cedente.Codigo, 11);

            if (string.IsNullOrEmpty(codigoCedente))
                throw new BoletoNetException("C�digo do cedente deve ser informado, " + infoFormatoCodigoCedente);

            var conta = boleto.Cedente.ContaBancaria.Conta;
            if (boleto.Cedente.ContaBancaria != null &&
                (!codigoCedente.StartsWith(boleto.Cedente.ContaBancaria.Agencia) ||
                 !(codigoCedente.EndsWith(conta) || codigoCedente.EndsWith(conta.Substring(0, conta.Length - 1)))))
                //throw new BoletoNetException("C�digo do cedente deve estar no " + infoFormatoCodigoCedente);
                boleto.Cedente.Codigo = string.Format("{0}{1}{2}", boleto.Cedente.ContaBancaria.Agencia, boleto.Cedente.ContaBancaria.OperacaConta, boleto.Cedente.Codigo);

            if (string.IsNullOrEmpty(boleto.Carteira))
                throw new BoletoNetException("Tipo de carteira � obrigat�rio. " + ObterInformacoesCarteirasDisponiveis());

            if (!CarteiraValida(boleto.Carteira))
                throw new BoletoNetException("Carteira informada � inv�lida. Informe " + ObterInformacoesCarteirasDisponiveis());

            MontaNossoNumero(boleto);

            FormataCodigoBarra(boleto);
            if (boleto.CodigoBarra.Codigo.Length != 44)
                throw new BoletoNetException("C�digo de barras � inv�lido");

            FormataLinhaDigitavel(boleto);
            FormataNossoNumero(boleto);
        }

        private string ObterInformacoesCarteirasDisponiveis()
        {
            return "";// string.Join(", ", carteirasDisponiveis.Select(o => string.Format("�{0}� � {1}", o.Key, o.Value)));
        }

        private bool CarteiraValida(string carteira)
        {
            int tipoCarteira;
            if (int.TryParse(carteira, out tipoCarteira))
            {
                return carteirasDisponiveis.ContainsKey(tipoCarteira);
            }
            return false;
        }

        public override void FormataNossoNumero(Boleto boleto)
        {
            string nossoNumero = boleto.NossoNumero;

            if (nossoNumero == null || nossoNumero.Length != 9)
            {
                MontaNossoNumero(boleto);
            }

            try
            {
                boleto.NossoNumero = string.Format("{0}/{1}-{2}", nossoNumero.Substring(0, 2), nossoNumero.Substring(2, 6), nossoNumero.Substring(8));
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao formatar nosso n�mero", ex);
            }
        }

        public override void MontaNossoNumero(Boleto boleto)
        {

            //Verifica se o nosso n�mero � v�lido
            var Length_NN = boleto.NossoNumero.Length;
            switch (Length_NN)
            {
                case 9: //Completo, recalcula o dv
                    boleto.NossoNumero = boleto.NossoNumero.Substring(0, Length_NN - 1);
                    boleto.DigitoNossoNumero = DigNossoNumeroSicredi(boleto);
                    boleto.NossoNumero += boleto.DigitoNossoNumero;
                    break;
                case 8: //Ano Sequencial
                    boleto.DigitoNossoNumero = DigNossoNumeroSicredi(boleto);
                    boleto.NossoNumero += boleto.DigitoNossoNumero;
                    break;
                case 6: //Sequencial
                    boleto.NossoNumero = DateTime.Now.ToString("yy") + boleto.NossoNumero;
                    boleto.DigitoNossoNumero = DigNossoNumeroSicredi(boleto);
                    boleto.NossoNumero += boleto.DigitoNossoNumero;
                    break;
                default:
                    throw new NotImplementedException("Nosso n�mero inv�lido! Deve possuir 6 posi��es");
            }
        }

        public override void FormataNumeroDocumento(Boleto boleto)
        {
            throw new NotImplementedException("Fun��o do fomata n�mero do documento n�o implementada.");
        }
        public override void FormataLinhaDigitavel(Boleto boleto)
        {
            //041M2.1AAAd1  CCCCC.CCNNNd2  NNNNN.041XXd3  V FFFF9999999999

            string campo1 = "7489" + boleto.CodigoBarra.Codigo.Substring(19, 5);
            int d1 = Mod10Sicredi(campo1);
            campo1 = FormataCampoLD(campo1) + d1.ToString();

            string campo2 = boleto.CodigoBarra.Codigo.Substring(24, 10);
            int d2 = Mod10Sicredi(campo2);
            campo2 = FormataCampoLD(campo2) + d2.ToString();

            string campo3 = boleto.CodigoBarra.Codigo.Substring(34, 10);
            int d3 = Mod10Sicredi(campo3);
            campo3 = FormataCampoLD(campo3) + d3.ToString();

            string campo4 = boleto.CodigoBarra.Codigo.Substring(4, 1);

            string campo5 = boleto.CodigoBarra.Codigo.Substring(5, 14);

            boleto.CodigoBarra.LinhaDigitavel = campo1 + "  " + campo2 + "  " + campo3 + "  " + campo4 + "  " + campo5;
        }
        private string FormataCampoLD(string campo)
        {
            return string.Format("{0}.{1}", campo.Substring(0, 5), campo.Substring(5));
        }

        public override void FormataCodigoBarra(Boleto boleto)
        {
            string valorBoleto = boleto.ValorBoleto.ToString("f").Replace(",", "").Replace(".", "");
            valorBoleto = Utils.FormatCode(valorBoleto, 10);

            var codigoCobranca = 1; //C�digo de cobran�a com registro
            string cmp_livre =
                codigoCobranca +
                boleto.Carteira +
                Utils.FormatCode(boleto.NossoNumero, 9) +
                Utils.FormatCode(boleto.Cedente.Codigo, 11) + "10";

            string dv_cmpLivre = digSicredi(cmp_livre).ToString();

            var codigoTemp = GerarCodigoDeBarras(boleto, valorBoleto, cmp_livre, dv_cmpLivre);

            boleto.CodigoBarra.CampoLivre = cmp_livre;
            boleto.CodigoBarra.FatorVencimento = FatorVencimento(boleto);
            boleto.CodigoBarra.Moeda = 9;
            boleto.CodigoBarra.ValorDocumento = valorBoleto;

            int _dacBoleto = digSicredi(codigoTemp);

            if (_dacBoleto == 0 || _dacBoleto > 9)
                _dacBoleto = 1;

            boleto.CodigoBarra.Codigo = GerarCodigoDeBarras(boleto, valorBoleto, cmp_livre, dv_cmpLivre, _dacBoleto);
        }

        private string GerarCodigoDeBarras(Boleto boleto, string valorBoleto, string cmp_livre, string dv_cmpLivre, int? dv_geral = null)
        {
            return string.Format("{0}{1}{2}{3}{4}{5}{6}",
                Utils.FormatCode(Codigo.ToString(), 3),
                boleto.Moeda,
                dv_geral.HasValue ? dv_geral.Value.ToString() : string.Empty,
                FatorVencimento(boleto),
                valorBoleto,
                cmp_livre,
                dv_cmpLivre);
        }

        //public bool RegistroByCarteira(Boleto boleto)
        //{
        //    bool valida = false;
        //    if (boleto.Carteira == "112"
        //        || boleto.Carteira == "115"
        //        || boleto.Carteira == "104"
        //        || boleto.Carteira == "147"
        //        || boleto.Carteira == "188"
        //        || boleto.Carteira == "108"
        //        || boleto.Carteira == "109"
        //        || boleto.Carteira == "150"
        //        || boleto.Carteira == "121")
        //        valida = true;
        //    return valida;
        //}

        #region M�todos de Gera��o do Arquivo de Remessa
        public override string GerarDetalheRemessa(Boleto boleto, int numeroRegistro, TipoArquivo tipoArquivo)
        {
            try
            {
                string _detalhe = " ";

                //base.GerarDetalheRemessa(boleto, numeroRegistro, tipoArquivo);

                switch (tipoArquivo)
                {
                    case TipoArquivo.CNAB240:
                        _detalhe = GerarDetalheRemessaCNAB240(boleto, numeroRegistro, tipoArquivo);
                        break;
                    case TipoArquivo.CNAB400:
                        _detalhe = GerarDetalheRemessaCNAB400(boleto, numeroRegistro, tipoArquivo);
                        break;
                    case TipoArquivo.Outro:
                        throw new Exception("Tipo de arquivo inexistente.");
                }

                return _detalhe;

            }
            catch (Exception ex)
            {
                throw new Exception("Erro durante a gera��o do DETALHE arquivo de REMESSA.", ex);
            }
        }
        public override string GerarHeaderRemessa(string numeroConvenio, Cedente cedente, TipoArquivo tipoArquivo, int numeroArquivoRemessa, Boleto boletos)
        {
            throw new NotImplementedException("Fun��o n�o implementada.");
        }

        public string GerarDetalheRemessaCNAB240(Boleto boleto, int numeroRegistro, TipoArquivo tipoArquivo)
        {
            try
            {

                TRegistroEDI reg = new TRegistroEDI();

                /*
                //01.1 Controle Banco C�digo do banco na compensa��o 1 3 3 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 003, 0, base.Codigo, '0'));

                //02.1 Lote Lote de servi�o 4 7 4 - Num. Preencher com '0001' para o primeiro lote do arquivo. Para os demais:
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0004, 004, 0, "0001", '0'));

                //03.1 Registro Tipo de registro 8 8 1 - Num '1' 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0008, 001, 0, "1", '0'));

                //04.1 Servi�o Opera��o Tipo de opera��o 9 9 1 - Alfa �R� = Arquivo remessa
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0009, 001, 0, "R", ' '));
                */

                //01.3P Controle Banco C�digo do banco na compensa��o 1 3 3 Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 003, 0, base.Codigo, '0'));

                //02.3P Lote Lote de servi�o 4 7 4 Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0004, 004, 0, "0001", '0'));

                //03.3P Registro Tipo de registro 8 8 1 '3' = Detalhe Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0008, 001, 0, "3", '0'));

                //04.3P N� do registro N� sequencial do registro no lote 9 13 5 Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0009, 005, 0, numeroRegistro, '0'));

                //05.3P Segmento C�d. segmento do registro detalhe 14 14 1 - Alfa 'P' 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0014, 001, 0, "P", ' '));

                //06.3P CNAB Uso exclusivo FEBRABAN/CNAB 15 15 1 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0015, 001, 0, "", ' '));

                //07.3P C�d. Mov. C�digo de movimento remessa 16 17 2 - Alfa
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0016, 002, 0, boleto.Remessa.CodigoOcorrencia, ' '));





                //08.3P C�digo Ag�ncia mantenedora da conta 18 22 5 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0018, 005, 0, boleto.Cedente.ContaBancaria.Agencia, '0'));

                //09.3P DV D�gito verificador da ag�ncia 23 23 1 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0023, 001, 0, boleto.Cedente.ContaBancaria.DigitoAgencia, ' '));

                //10.3P N�mero da conta corrente 24 35 12 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0024, 012, 0, boleto.Cedente.ContaBancaria.Conta, '0'));

                //11.3P DV D�gito verificador da conta 36 36 1 - Alfa
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0036, 001, 0, boleto.Cedente.ContaBancaria.DigitoConta, ' '));

                //12.3P DV D�gito verificador da coop/ag/conta 37 37 1 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0037, 001, 0, boleto.Cedente.ContaBancaria.DigitoAgenciaConta, ' '));

                //13.3P Nosso n�mero Identifica��o do t�tulo no banco 38 57 20 - Alfa 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0038, 020, 0, boleto.NossoNumero, ' '));

                //14.3P Carteira C�digo da carteira 58 58 1 - Alfa '1' = Cobran�a Simples
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0058, 001, 0, "1", ' '));

                //15.3P Forma de cad. do t�tulo no banco 59 59 1 - Num Dom�nio: '1' = Com cadastramento (cobran�a registrada) '2' = Sem cadastramento (cobran�a sem registro) Obs.: destina-se somente para emiss�o de boleto pelo banco
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0059, 001, 0, "1", '0'));

                //16.3P Documento Tipo de documento 60 60 1 - Alfa Dom�nio: '1' = Tradicional '2' = Escritural Obs.: O Sicredi n�o realizar� diferencia��o entre os dom�nios.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0060, 001, 0, "2", ' '));

                //17.3P Emiss�o boleto Ident. emiss�o do boleto 61 61 1 - Alfa Dom�nio: '1' = Sicredi emite (auto-envelop�vel) '2' = Benefici�rio emite
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0061, 001, 0, "2", ' '));

                //18.3P Identifica��o da distribui��o 62 62 1 - Alfa Dom�nio: '1' = Sicredi distribui '2' = Benefici�rio distribui
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0062, 001, 0, "2", ' '));

                //19.3P N� do documento N� do documento de cobran�a 63 77 15 - Alfa
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0063, 015, 0, boleto.NumeroDocumento, ' '));

                //20.3P Vencimento Data de vencimento do t�tulo 78 85 8 - Num (formato DDMMAAAA)
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0078, 008, 0, boleto.DataVencimento.ToString("ddMMyyyy"), '0'));

                //21.3P Valor do t�tulo Valor nominal do t�tulo 86 100 13/2 Num (utilizar 2 decimais)
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0086, 015, 2, boleto.ValorBoleto, '0'));

                //22.3P Cooperativa / agencia cobradora 101 105 5 - Num Zeros cobran�a
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0101, 005, 0, "0", '0'));

                //23.3P DV D�gito verificador da coop./ag�ncia 106 106 1 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0106, 001, 0, "", ' '));

                #region Especie

                //EspecieDocumento_Sicredi

                string especieDoc = "03";
                switch (EspecieDocumento_Sicredi.getEnumEspecieByCodigo(boleto.EspecieDocumento.Codigo))
                {
                    case EnumEspecieDocumento_Sicredi.DuplicataMercantilIndicacao:
                        especieDoc = "03";
                        break;
                    case EnumEspecieDocumento_Sicredi.DuplicataRural:
                        especieDoc = "06";
                        break;
                    case EnumEspecieDocumento_Sicredi.NotaPromissoria:
                        especieDoc = "12";
                        break;
                    case EnumEspecieDocumento_Sicredi.NotaPromissoriaRural:
                        especieDoc = "13";
                        break;
                    case EnumEspecieDocumento_Sicredi.NotaSeguros:
                        especieDoc = "16";
                        break;
                    case EnumEspecieDocumento_Sicredi.Recibo:
                        especieDoc = "17";
                        break;
                    case EnumEspecieDocumento_Sicredi.LetraCambio:
                        especieDoc = "07";
                        break;
                    case EnumEspecieDocumento_Sicredi.NotaDebito:
                        especieDoc = "19";
                        break;
                    case EnumEspecieDocumento_Sicredi.DuplicataServicoIndicacao:
                        especieDoc = "05";
                        break;
                    case EnumEspecieDocumento_Sicredi.Outros:
                        especieDoc = "99";
                        break;
                }

                #endregion

                //24.3P Esp�cie de t�tulo Esp�cie do t�tulo 107 108 2 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0107, 002, 0, especieDoc, '0'));

                //25.3P Aceite Identifica��o de t�tulo aceito/n�o 109 109 1 - Alfa aceito Dom�nio: 'A' = Aceite 'N' = N�o aceite
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0109, 001, 0, boleto.Aceite, ' '));

                //26.3P Data emiss�o do t�tulo Data da emiss�o do t�tulo 110 117 8 alfa (formato DDMMAAAA)
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0110, 008, 0, boleto.DataDocumento.ToString("ddMMyyyy"), ' '));

                #region C�digo de juros
                string codJurosMora = "3"; //Isento de Juros
                decimal jurosMora = 0;
                if (boleto.JurosMora > 0)
                {
                    jurosMora = boleto.JurosMora;
                    codJurosMora = "1"; //  Valor por Dia
                }
                else if (boleto.PercJurosMora > 0)
                {
                    jurosMora = boleto.PercJurosMora;
                    codJurosMora = "2"; // Percentual por M�s
                }
                #endregion

                //27.3P C�d. juros mora C�digo do juro de mora 118 118 1 - Num Dom�nio: '1' = Valor por dia �2� = Taxa Mensal '3' = Isento
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0118, 001, 0, codJurosMora, '0'));

                //28.3P Data de juros Data do juro de mora 119 126 8 - Num Zeros Caso seja inv�lida ou n�o informada, ser� assumida a data do vencimento.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0119, 008, 0, boleto.DataJurosMora, '0'));

                //29.3P Juros mora Juros de mora por dia/taxa 127 141 13 2 Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0127, 015, 2, jurosMora, '0'));

                //30.3P C�d. desc. 1 C�digo do desconto 1 142 142 1 - Num C�digo do desconto
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0142, 001, 0, (boleto.ValorDesconto > 0)? '1':'0', '0'));

                //31.3P Data desc. 1 Data do desconto 1 143 150 8 - Num (formato DDMMAAAA)
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0143, 008, 0, boleto.DataDesconto, '0'));

                //32.3P Desconto 1 Valor percentual a ser concedido 151 165 13 2 Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0151, 015, 2, boleto.ValorDesconto, '0'));

                //33.3P Vlr IOF Valor do IOF a ser recolhido 166 180 13 2 Num Zeros O Sicredi atualmente n�o utiliza este campo.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0166, 015, 2, boleto.IOF, '0'));

                //34.3P Vlr abatimento Valor do abatimento 181 195 13 2 Num Informar valor do abatimento (alinhado � direita e zeros � esquerda) ou preencher com zeros.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0181, 015, 2, boleto.Abatimento, '0'));

                //35.3P Uso empresa benefici�ria Identifica��o do t�tulo na empresa 196 220 25 - Alfa O Sicredi atualmente n�o utiliza este campo.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0196, 025, 0, boleto.NumeroControle, ' '));

                #region CodProtesto codBaixaDevolucao
                String codProtesto = "3";
                int diasProtesto = 0;
                foreach (IInstrucao instrucao in boleto.Instrucoes)
                {
                    switch ((EnumInstrucoes_Sicredi)instrucao.Codigo)
                    {
                        case EnumInstrucoes_Sicredi.PedidoProtesto:
                            codProtesto = "1";
                            diasProtesto = instrucao.QuantidadeDias;
                            break;
                    }
                }
                #endregion

                //36.3P C�digo para protesto/negativa��o C�digo para protesto/negativa��o 221 221 1 Num Dom�nio: �1� = Protestar dias corridos '3' = N�o protestar/negativar �8� = Negativa��o sem Protesto '9' = Cancelamento protesto autom�tico/negativa��o
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0221, 001, 0, codProtesto, '0'));

                //37.3P Prazo para protesto/negativa��o N�mero de dias para 222 223 2 - Num M�nimo 3 dias
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0222, 002, 0, diasProtesto, '0'));

                //38.3P C�digo p/ baixa / devolu��o C�digo para baixa/devolu��o 224 224 1 C�digo para baixa / devolu��o Utilizar sempre dom�nio �1� para esse campo.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0224, 001, 0, "1", '0'));

                //39.3P Prazo p / baixa / devolu��o N� de dias para baixa/devolu��o 225 227 3 - Alfa Utilizar sempre, nesse campo, 60 dias para baixa/devolu��o.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0225, 003, 0, "060", ' '));

                //40.3P C�digo da moeda C�digo da moeda 228 229 2 - Num �09� Dom�nio: '09' = Real
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 228, 002, 0, boleto.Moeda, '0'));

                //41.3P N�mero do contrato N� do contrato da opera��o de cr�d. 230 239 10 - Num Zeros (C030) O Sicredi atualmente n�o utiliza este campo
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0230, 010, 0, 0, '0'));

                //42.3P CNAB Uso exclusivo FEBRABAN/CNAB 240 240 1 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0240, 001, 0, "", ' '));

                reg.CodificarLinha();
                //
                string vLinha = reg.LinhaRegistro;
                string _segmentoP = Utils.SubstituiCaracteresEspeciais(vLinha);

                return _segmentoP;

                /*
                string detalhe = Utils.FormatCode(Codigo.ToString(), "0", 3, true);
                detalhe += Utils.FormatCode("", "0", 4, true);
                detalhe += "3";
                detalhe += Utils.FormatCode(numeroRegistro.ToString(), 5);
                detalhe += "P 01";
                detalhe += Utils.FormatCode(boleto.Cedente.ContaBancaria.Agencia, 5);
                detalhe += "0";
                detalhe += Utils.FormatCode(boleto.Cedente.ContaBancaria.Conta, 12);
                detalhe += boleto.Cedente.ContaBancaria.DigitoConta;
                detalhe += " ";
                detalhe += Utils.FormatCode(boleto.NossoNumero.Replace("/", "").Replace("-", ""), 20);
                detalhe += "1";
                detalhe += (Convert.ToInt16(boleto.Carteira) == 1 ? "1" : "2");
                detalhe += "122";
                detalhe += Utils.FormatCode(boleto.NumeroDocumento, 15);
                detalhe += boleto.DataVencimento.ToString("ddMMyyyy");
                string valorBoleto = boleto.ValorBoleto.ToString("f").Replace(",", "").Replace(".", "");
                valorBoleto = Utils.FormatCode(valorBoleto, 13);
                detalhe += valorBoleto;
                detalhe += "00000 99A";
                detalhe += boleto.DataDocumento.ToString("ddMMyyyy");
                detalhe += "200000000";
                valorBoleto = boleto.JurosMora.ToString("f").Replace(",", "").Replace(".", "");
                valorBoleto = Utils.FormatCode(valorBoleto, 13);
                detalhe += valorBoleto;
                detalhe += "1";
                detalhe += boleto.DataDesconto.ToString("ddMMyyyy");
                valorBoleto = boleto.ValorDesconto.ToString("f").Replace(",", "").Replace(".", "");
                valorBoleto = Utils.FormatCode(valorBoleto, 13);
                detalhe += valorBoleto;
                detalhe += Utils.FormatCode("", 26);
                detalhe += Utils.FormatCode("", " ", 25);
                detalhe += "0001060090000000000 ";

                detalhe = Utils.SubstituiCaracteresEspeciais(detalhe);

                return detalhe;
                */
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao gerar DETALHE do arquivo CNAB240.", e);
            }
        }

        public override string GerarHeaderRemessa(Cedente cedente, TipoArquivo tipoArquivo, int numeroArquivoRemessa)
        {
            return GerarHeaderRemessa("0", cedente, tipoArquivo, numeroArquivoRemessa);
        }

        public override string GerarHeaderRemessa(string numeroConvenio, Cedente cedente, TipoArquivo tipoArquivo, int numeroArquivoRemessa)
        {
            try
            {
                string _header = " ";

                //base.GerarHeaderRemessa("0", cedente, tipoArquivo, numeroArquivoRemessa);

                switch (tipoArquivo)
                {

                    case TipoArquivo.CNAB240:
                        _header = GerarHeaderRemessaCNAB240(cedente, numeroArquivoRemessa);
                        break;
                    case TipoArquivo.CNAB400:
                        _header = GerarHeaderRemessaCNAB400(0, cedente, numeroArquivoRemessa);
                        break;
                    case TipoArquivo.Outro:
                        throw new Exception("Tipo de arquivo inexistente.");
                }

                return _header;

            }
            catch (Exception ex)
            {
                throw new Exception("Erro durante a gera��o do HEADER do arquivo de REMESSA.", ex);
            }
        }

        public override string GerarDetalheSegmentoQRemessa(Boleto boleto, int numeroRegistro, TipoArquivo tipoArquivo)
        {
            try
            {
                TRegistroEDI reg = new TRegistroEDI();

                //01.3Q Controle Banco C�digo do banco na compensa��o 1 3 3 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 003, 0, base.Codigo, '0'));                                   // posi��o 001-003 (003) - c�digo do banco na compensa��o

                //02.3Q Lote Lote de servi�o 4 7 4 - Num "0001"
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0004, 004, 0, "0001", '0'));                                           // posi��o 004-007 (004) - Lote de Servi�o

                //03.3Q Registro Tipo de registro 8 8 1 - Num '3' = Detalhe
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0008, 001, 0, "3", '0'));                                           // posi��o 008-008 (001) - Tipo de Registro

                //04.3Q N� sequencial do registro no lote 9 13 5 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0009, 005, 0, numeroRegistro, '0'));                                // posi��o 009-013 (005) - N� Sequencial do Registro no Lote

                //05.3Q Segmento C�d. segmento do registro detalhe 14 14 1 - Alfa �Q� 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0014, 001, 0, "Q", '0'));                                           // posi��o 014-014 (001) - C�d. Segmento do Registro Detalhe

                //06.3Q CNAB Uso exclusivo FEBRABAN/CNAB 15 15 1 - Alfa Brancos 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0015, 001, 0, string.Empty, ' '));                                  // posi��o 015-015 (001) - Uso Exclusivo FEBRABAN/CNAB

                //07.3Q C�d. Mov. C�digo de movimento remessa 16 17 2 - Alfa 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0016, 002, 0, boleto.Remessa.CodigoOcorrencia, '0'));                                          // posi��o 016-017 (002) - C�digo de Movimento Remessa

                //08.3Q Dados Inscri��o Tipo Tipo de inscri��o 18 18 1 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0018, 001, 0, boleto.Sacado.CPFCNPJ.Length > 11 ? "2" : "1", '0'));                                   // posi��o 018-018 (001) - Tipo de Inscri��o 

                //09.3Q N�mero N�mero de inscri��o 19 33 15 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0019, 015, 0, boleto.Sacado.CPFCNPJ, '0'));                         // posi��o 019-033 (015) - N�mero de Inscri��o da empresa

                //10.3Q do Nome Nome 34 73 40 - Alfa Nome que identifica a pessoa, f�sica ou jur�dica, a qual se quer fazer refer�ncia.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0034, 040, 0, boleto.Sacado.Nome.ToUpper(), ' '));

                //11.3Q Endere�o Endere�o 74 113 40 - Alfa 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0074, 040, 0, boleto.Sacado.Endereco.End.ToUpper(), ' '));

                //12.3Q Bairro Bairro 114 128 15 - Alfa 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0114, 015, 0, boleto.Sacado.Endereco.Bairro.ToUpper(), ' '));

                //13.3Q CEP CEP 129 133 5 - Num 
                //14.3Q Sufixo do CEP Sufixo do CEP 134 136 3 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0129, 008, 0, boleto.Sacado.Endereco.CEP, ' '));

                //15.3Q Cidade Cidade 137 151 15 - Alfa 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0137, 015, 0, boleto.Sacado.Endereco.Cidade.ToUpper(), ' '));

                //16.3Q UF Unidade da Federa��o 152 153 2 - Alfa 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0152, 002, 0, boleto.Sacado.Endereco.UF, ' '));

                if (boleto.Avalista == null){
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0154, 001, 0, "0", '0'));
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0155, 015, 0, "0", '0'));
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0170, 040, 0, string.Empty, ' '));
                }else
                {
                    //17.3Q Sac. / Inscri��o Tipo Tipo de inscri��o 154 154 1 - Num 
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0154, 001, 0, boleto.Avalista.CPFCNPJ.Length == 11 ? "1" : "2", '0'));

                    //18.3Q N�mero N�mero de inscri��o 155 169 15 - Num 
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0155, 015, 0, boleto.Avalista.CPFCNPJ, '0'));

                    //19.3Q Nome Nome do Sacador avalista 170 209 40 - Alfa Nome que identifica a pessoa, f�sica ou jur�dica, a qual se quer fazer refer�ncia.
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0170, 040, 0, boleto.Avalista.Nome, ' '));
                }

                //20.3Q Banco correspondente C�d. bco corresp. na compensa��o 210 212 3 - Num Zeros
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0210, 003, 0, "000", '0'));

                //21.3Q Nosso num. banco Nosso n� no banco correspondente 213 232 20 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0213, 020, 0, string.Empty, ' '));

                //22.3Q CNAB Uso exclusivo FEBRABAN/CNAB 233 240 8 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0233, 008, 0, string.Empty, ' '));
                
                reg.CodificarLinha();

                string vLinha = reg.LinhaRegistro;
                string _segmentoQ = Utils.SubstituiCaracteresEspeciais(vLinha);

                return _segmentoQ;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro durante a gera��o do HEADER DO LOTE do arquivo de REMESSA.", ex);
            }
        }

        public override string GerarHeaderLoteRemessa(string numeroConvenio, Cedente cedente, int numeroArquivoRemessa, TipoArquivo tipoArquivo)
        {
            try
            {
                string header = " ";

                switch (tipoArquivo)
                {

                    case TipoArquivo.CNAB240:
                        header = GerarHeaderLoteCNAB240(cedente, numeroArquivoRemessa);
                        break;
                    case TipoArquivo.CNAB400:
                        // n�o tem no CNAB 400 header = GerarHeaderLoteRemessaCNAB400(0, cedente, numeroArquivoRemessa);
                        break;
                    case TipoArquivo.Outro:
                        throw new Exception("Tipo de arquivo inexistente.");
                }

                return header;

            }
            catch (Exception ex)
            {
                throw new Exception("Erro durante a gera��o do HEADER DO LOTE do arquivo de REMESSA.", ex);
            }
        }

        public string GerarHeaderLoteCNAB240(Cedente cedente, int numeroArquivoRemessa)
        {
            try
            {

                TRegistroEDI reg = new TRegistroEDI();

                //01.1 Controle Banco C�digo do banco na compensa��o 1 3 3 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 003, 0, base.Codigo, '0'));

                //02.1 Lote Lote de servi�o 4 7 4 - Num. Preencher com '0001' para o primeiro lote do arquivo. Para os demais:
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0004, 004, 0, "0001", '0'));

                //03.1 Registro Tipo de registro 8 8 1 - Num '1' 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0008, 001, 0, "1", '0'));

                //04.1 Servi�o Opera��o Tipo de opera��o 9 9 1 - Alfa �R� = Arquivo remessa
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0009, 001, 0, "R", ' '));

                //05.1 Servi�o Tipo de servi�o 10 11 2 - Num '01' 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0010, 002, 0, "1", '0'));

                //06.1 CNAB Uso exclusivo FEBRABAN/CNAB 12 13 2 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0012, 002, 0, "", ' '));

                //07.1 Leiaute do lote N� da vers�o do leiaute do lote 14 16 3 - Num '040'
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0014, 003, 0, "040", '0'));

                //08.1 CNAB Uso exclusivo FEBRABAN/CNAB 17 17 1 - Alfa Brancos 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0017, 001, 0, "", ' '));

                //09.1 Tipo Tipo de inscri��o da empresa 18 18 1 - Num. '1' = CPF e '2' = CGC / CNPJ
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 00018, 001, 0, cedente.CPFCNPJ.Length == 11 ? "1" : "2", ' '));

                //10.1 N�mero N� de inscri��o da empresa 19 33 15 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0019, 015, 0, cedente.CPFCNPJ, '0'));

                //11.1 C�digo do conv�nio no banco 34 53 20 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0034, 020, 0, "", ' '));

                //12.1 C/ Ag�ncia C Ag�ncia mantenedora da conta 54 58 5 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0054, 005, 0, cedente.ContaBancaria.Agencia, '0'));

                //13.1 D�gito verificador da ag�ncia 59 59 1 - Alfa Brancos cooperativa de cr�dito/ag�ncia. Campo espec�fico para o DV. 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0059, 001, 0, cedente.ContaBancaria.DigitoAgencia, ' '));

                //14.1 C�digo da conta corrente do associado 60 71 12 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0060, 012, 0, cedente.ContaBancaria.Conta, '0'));

                //15.1 DV Digito verificador (DV) da conta - 72 72 1 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0072, 001, 0, cedente.ContaBancaria.DigitoConta, '0'));

                //16.1 DV D�gito verificador da coop/ag/conta 73 73 1 - Alfa (G012) O Sicredi atualmente n�o utiliza este campo
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0073, 001, 0, "", ' '));

                //17.1 Nome Nome da empresa 74 103 30 - Alfa Nome que identifica a pessoa, f�sica ou jur�dica, a qual se quer fazer refer�ncia.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0074, 030, 0, cedente.Nome, ' '));

                //18.1 Informa��o 1 Mensagem 1 104 143 40 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0104, 040, 0, "", ' '));

                //19.1 Informa��o 2 Mensagem 2 144 183 40 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0144, 040, 0, "", ' '));

                //20.1 Controle da N� N�mero remessa/retorno 184 191 8 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0184, 008, 0, numeroArquivoRemessa, '0'));

                //21.1 Dt. Data de grava��o rem./ret. 192 199 8 - Num (formato DDMMAAAA)
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0192, 008, 0, DateTime.Now.ToString("ddMMyyyy"), '0'));

                //22.1 Data do Cr�dito Data do cr�dito 200 207 8 - Num Zeros (formato DDMMAAAA) Obs.: o Sicredi n�o utilizar� esse campo.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0200, 008, 0, "0", '0'));

                //23.1 CNAB Uso exclusivo FEBRABAN/CNAB 208 240 33 - Alfa Brancos 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0208, 033, 0, "", ' '));

                reg.CodificarLinha();
                //
                string vLinha = reg.LinhaRegistro;
                string _HeaderLote = Utils.SubstituiCaracteresEspeciais(vLinha);

                return _HeaderLote;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar HEADER do arquivo de remessa do CNAB240.", ex);
            }
        }

        public string GerarHeaderRemessaCNAB240(Cedente cedente, int numeroArquivoRemessa)
        {
            try
            {

                TRegistroEDI reg = new TRegistroEDI();

                //01.0 Controle Banco C�digo do banco na compensa��o 1 3 3 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 003, 0, base.Codigo, '0'));

                //02.0 Lote Lote de servi�o 4 7 4 - Num �0000�
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0004, 004, 0, "0000", '0'));

                //03.0 Registro Tipo de registro 8 8 1 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0008, 001, 0, "0", '0'));

                //04.0 CNAB Uso exclusivo FEBRABAN/CNAB 9 17 9 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0009, 009, 9, "", ' '));

                //05.0 Tipo Tipo de inscri��o da empresa 18 18 1 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 00018, 001, 0, cedente.CPFCNPJ.Length == 11 ? "1" : "2", ' '));

                //06.0 N�mero N� de inscri��o da empresa 19 32 14 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0019, 014, 0, cedente.CPFCNPJ, '0'));

                //07.0 Conv�nio C�digo do conv�nio no banco 33 52 20 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0033, 020, 0, "", ' '));

                //08.0 Ag�ncia C�digo Ag�ncia mantenedora da conta 53 57 5 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0053, 005, 0, cedente.ContaBancaria.Agencia, '0'));

                //09.0 Corrente DV D�gito verificador da ag�ncia 58 58 1 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0058, 001, 0, cedente.ContaBancaria.DigitoAgencia, ' '));

                //10.0 C�digo Conta Corrente do Benefici�rio 59 70 12 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0059, 012, 0, cedente.ContaBancaria.Conta, '0'));

                //11.0 Benefici�rio DV D�gito verificador da conta 71 71 1 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0071, 001, 0, cedente.ContaBancaria.DigitoConta, '0'));

                //12.0 N�o utilizado 72 72 1 - Alfa Brancos 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0072, 001, 0, "", ' '));

                //13.0 Nome da empresa 73 102 30 - Alfa
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0073, 030, 0, cedente.Nome, ' '));

                //14.0 Nome do Banco Nome do Banco 103 132 30 - Alfa SICREDI
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0103, 030, 0, "SICREDI", ' '));

                //15.0 CNAB Uso exclusivo FEBRABAN/CNAB 133 142 10 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0133, 010, 0, "", ' '));

                //16.0 C�digo remessa / retorno 143 143 1 - Num �1� = Remessa 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0143, 001, 0, "1", '0'));

                //17.0 Data de Gera��o Data de gera��o do arquivo 144 151 8 - Num (Utilizar o formato DDMMAAAA)
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0144, 008, 0, DateTime.Now.ToString("ddMMyyyy"), '0'));

                //18.0 Hora de Gera��o Hora de gera��o do arquivo 152 157 6 - Num (Utilizar o formato HHMMSS)
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0152, 006, 0, DateTime.Now.ToString("HHmmss"), '0'));

                //19.0 Sequencia (NSA) N�mero sequencial do arquivo 158 163 6 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0158, 006, 0, numeroArquivoRemessa, '0'));

                //20.0 Leiaute do Arquivo N� da vers�o do leiaute do arquivo 164 166 3 - Num �081�
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0164, 003, 0, "081", '0'));

                //21.0 Densidade Densidade de grava��o do arquivo 167 171 5 - Num �01600� 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0167, 005, 0, "01600", '0'));

                //22.0 Reservado Banco Para uso reservado do Banco 172 191 20 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0172, 0020, 0, "", ' '));

                //23.0 Reservado Empresa Para uso reservado da Empresa 192 211 20 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0192, 020, 0, "", ' '));

                //24.0 CNAB Uso exclusivo FEBRABAN/CNAB 212 240 29 - Alfa Brancos
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0212, 029, 0, "", ' '));

                reg.CodificarLinha();
                //
                string vLinha = reg.LinhaRegistro;
                string _Header = Utils.SubstituiCaracteresEspeciais(vLinha);

                return _Header;
                /*
                string header = "748";
                header += "0000";
                header += "0";
                header += Utils.FormatCode("", " ", 9);
                header += (cedente.CPFCNPJ.Length == 11 ? "1" : "2");
                header += Utils.FormatCode(cedente.CPFCNPJ, "0", 14, true);
                header += Utils.FormatCode(cedente.Convenio.ToString(), " ", 20);
                header += Utils.FormatCode(cedente.ContaBancaria.Agencia, "0", 5, true);
                header += " ";
                header += Utils.FormatCode(cedente.ContaBancaria.Conta, "0", 12, true);
                header += Utils.FormatCode(cedente.ContaBancaria.DigitoConta, " ", 1, true);
                header += " ";
                header += Utils.FormatCode(cedente.Nome, " ", 30);
                header += Utils.FormatCode("SICREDI", " ", 30);
                header += Utils.FormatCode("", " ", 10); //133 - 142 / 10
                //header += Utils.FormatCode(cedente.Nome, " ", 30);
                header += "1";
                header += DateTime.Now.ToString("ddMMyyyyHHmmss");
                header += Utils.FormatCode("", "0", 6); //Sequencial do arquivo? 158-163 / 6
                header += "081"; //082?
                header += "01600"; //167-171 / 5
                header += Utils.FormatCode("", " ", 69);
                header = Utils.SubstituiCaracteresEspeciais(header);
                return header;
                */
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar HEADER do arquivo de remessa do CNAB240.", ex);
            }
        }

        public override string GerarTrailerRemessa(int numeroRegistro, TipoArquivo tipoArquivo, Cedente cedente, decimal vltitulostotal)
        {
            try
            {
                string _trailer = " ";

                switch (tipoArquivo)
                {
                    case TipoArquivo.CNAB240:
                        _trailer = GerarTrailerRemessa240(numeroRegistro);
                        break;
                    case TipoArquivo.CNAB400:
                        _trailer = GerarTrailerRemessa400(numeroRegistro, cedente);
                        break;
                    case TipoArquivo.Outro:
                        throw new Exception("Tipo de arquivo inexistente.");
                }

                return _trailer;

            }
            catch (Exception ex)
            {
                throw new Exception("", ex);
            }
        }
        public override string GerarTrailerLoteRemessa(int numeroRegistro)
        {
            return GerarTrailerLoteRemessaCNAB240(numeroRegistro);
        }
        private string GerarTrailerLoteRemessaCNAB240(int numeroRegistro)
        {
            try
            {
                TRegistroEDI reg = new TRegistroEDI();

                //01.5 Controle Banco C�digo do banco na compensa��o 1 3 3 - Num
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 003, 0, base.Codigo, '0'));                // posi��o 001-003 (003) - c�digo do banco na compensa��o        

                //02.5 Lote Lote de servi�o 4 7 4 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0004, 004, 0, "1", '0'));                        // posi��o 004-007 (004) - Lote de Servi�o

                //03.5 Registro Tipo de registro 8 8 1 - Num '5' = Trailer de lote
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0008, 001, 0, "5", '0'));                        // posi��o 008-008 (001) - Tipo de Registro

                //04.5 CNAB Uso exclusivo FEBRABAN/CNAB 9 17 9 - Alfa Brancos 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0009, 009, 0, string.Empty, ' '));               // posi��o 009-017 (009) - Uso Exclusivo FEBRABAN/CNAB

                //05.5 Quantidade de registros Quantidade de registros no lote 18 23 6 - Num. Somat�ria dos registros de tipo 1, 2, 3, 4 e 5. Registros de tipo 2 e 4
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0018, 006, 0, numeroRegistro, '0'));             // posi��o 018-023 (006) - Quantidade de Registros no Lote

                //06.5 Totaliza��o da cobran�a simp. Quantidade de t�tulos em cobran�a 24 29 6 - Num Somat�ria dos registros enviados no lote do arquivo, de acordo com o c�digo da carteira. S� ser�o utilizados para informa��o do arquivo retorno.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0024, 006, 0, "0", '0'));                        // posi��o 024-029 (006) - Quantidade de T�tulos em Cobran�a

                //07.5 Valor total dos t�tulos em carteiras 30 46 15 2 Num Somat�ria dos valores dos t�tulos de cobran�a enviados no lote do arquivo de acordo com o c�digo da carteira.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0030, 017, 2, "0", '0'));                        // posi��o 030-046 (017) - Valor Total dos T�tulos em Carteiras

                //08.5 Totaliza��o da cobran�a vinculada Quantidade de t�tulos em cobran�a 47 52 6 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0047, 006, 0, "0", '0'));                        // posi��o 047-052 (006) - Quantidade de T�tulos em Cobran�a

                //09.5 Valor total dos t�tulos em carteiras 53 69 15 2 Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0053, 017, 2, "0", '0'));                        // posi��o 053-069 (017) - Valor Total dos T�tulos em Carteiras                

                //10.5Totaliza��o da cobran�a Quantidade de t�tulos em cobran�a 70 75 6 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0070, 006, 0, "0", '0'));                        // posi��o 070-075 (006) - Quantidade de T�tulos em Cobran�a

                //11.5 Quantidade de t�tulos em carteiras 76 92 15 2 Num Somat�ria dos valores dos t�tulos de cobran�a enviados no lote do arquivo de acordo com o c�digo da carteira. S� ser�o utilizados para informa��o do arquivo retorno.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0076, 017, 2, "0", '0'));                        // posi��o 076-092 (017) - Quantidade de T�tulos em Carteiras 

                //12.5Totaliza��o da cobran�a Quantidade de t�tulos em cobran�a 93 98 6 - Num Somat�ria dos registros enviados no lote do arquivo, de acordo com o c�digo da descontada carteira. S� ser�o utilizados para informa��o do arquivo retorno.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0093, 006, 0, "0", '0'));                        // posi��o 093-098 (006) - Quantidade de T�tulos em Cobran�a Descontadas

                //13.5 Valor total dos t�tulos em carteiras 99 115 15 2 Num Somat�ria dos valores dos t�tulos de cobran�a enviados no lote do arquivo de acordo com o c�digo da carteira. S� ser�o utilizados para informa��o do arquivo retorno.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0099, 017, 2, "0", '0'));                        // posi��o 099-115 (017) - Valor Total dosT�tulos em Carteiras Descontadas

                //14.5N. do aviso N�mero do aviso de lan�amento 116 123 8 - Alfa Brancos (C072) O Sicredi atualmente n�o utiliza este campo
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0116, 008, 0, string.Empty, ' '));               // posi��o 116-123 (008) - N�mero do Aviso de Lan�amento

                //15.5CNAB Uso exclusivo FEBRABAN/CNAB 124 240 117 - Alfa Brancos Texto de observa��es destinado para uso exclusivo do Sicredi. Preencher com brancos. Controle: banco origem ou destino do arquivo (banco benefici�rio).
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0124, 117, 0, string.Empty, ' '));               // posi��o 124-240 (117) - Uso Exclusivo FEBRABAN/CNAB                

                reg.CodificarLinha();

                string vLinha = reg.LinhaRegistro;
                string _trailerLote = Utils.SubstituiCaracteresEspeciais(vLinha);

                return _trailerLote;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar TRAILER do lote no arquivo de remessa do CNAB240.", ex);
            }
        }

        public string GerarTrailerRemessa240(int numeroRegistro)
        {
            try
            {
                TRegistroEDI reg = new TRegistroEDI();

                //01.9 Banco C�digo do banco na compensa��o 1 3 3 - Num 748 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 003, 0, base.Codigo, '0'));
                
                //02.9 Lote Lote de servi�o 4 7 4 - Num �9999� 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0004, 004, 0, "9999", '0'));
                
                //03.9 Registro Tipo de registro 8 8 1 - Num '9' = Trailer de arquivo
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0008, 001, 0, "9", '0'));
                
                //04.9 CNAB Uso exclusivo FEBRABAN/CNAB 9 17 9 - Alfa Brancos 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0009, 009, 0, string.Empty, ' '));
                
                //05.9 Qtde. de lotes Quantidade de lotes do arquivo 18 23 6 - Num 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0018, 006, 0, "1", '0'));
                
                //06.9 Qtde de registros Quantidade de registros do arquivo 24 29 6 - Num Totais Somat�ria dos registros de tipo 0, 1, 3, 5 e 9.
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0024, 006, 0, numeroRegistro, '0'));
                
                //07.9 Qtde de contas concil. Quantidade de contas p/ concil. (lotes) 30 35 6 - Num Zeros 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0030, 006, 0, "0", '0'));
                
                //08.9 CNAB Uso exclusivo FEBRABAN/CNAB 36 240 205 - Alfa Brancos 
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0036, 205, 0, string.Empty, ' '));

                reg.CodificarLinha();

                string vLinha = reg.LinhaRegistro;
                string _trailer = Utils.SubstituiCaracteresEspeciais(vLinha);

                return _trailer;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro durante a gera��o do registro TRAILER do arquivo de REMESSA.", ex);
            }
        }

        #endregion

        #region M�todos de Leitura do Arquivo de Retorno
        /*
         * Substitu�do M�todo de Leitura do Retorno pelo Interpretador de EDI;
        public override DetalheRetorno LerDetalheRetornoCNAB400(string registro)
        {
            try
            {
                DetalheRetorno detalhe = new DetalheRetorno(registro);

                int idRegistro = Utils.ToInt32(registro.Substring(0, 1));
                detalhe.IdentificacaoDoRegistro = idRegistro;

                detalhe.NossoNumero = registro.Substring(47, 15);

                int codigoOcorrencia = Utils.ToInt32(registro.Substring(108, 2));
                detalhe.CodigoOcorrencia = codigoOcorrencia;

                //Data Ocorr�ncia no Banco
                int dataOcorrencia = Utils.ToInt32(registro.Substring(110, 6));
                detalhe.DataOcorrencia = Utils.ToDateTime(dataOcorrencia.ToString("##-##-##"));

                detalhe.SeuNumero = registro.Substring(116, 10);

                int dataVencimento = Utils.ToInt32(registro.Substring(146, 6));
                detalhe.DataVencimento = Utils.ToDateTime(dataVencimento.ToString("##-##-##"));

                decimal valorTitulo = Convert.ToUInt64(registro.Substring(152, 13));
                detalhe.ValorTitulo = valorTitulo / 100;

                detalhe.EspecieTitulo = registro.Substring(174, 1);

                decimal despeasaDeCobranca = Convert.ToUInt64(registro.Substring(175, 13));
                detalhe.DespeasaDeCobranca = despeasaDeCobranca / 100;

                decimal outrasDespesas = Convert.ToUInt64(registro.Substring(188, 13));
                detalhe.OutrasDespesas = outrasDespesas / 100;

                decimal abatimentoConcedido = Convert.ToUInt64(registro.Substring(227, 13));
                detalhe.Abatimentos = abatimentoConcedido / 100;

                decimal descontoConcedido = Convert.ToUInt64(registro.Substring(240, 13));
                detalhe.Descontos = descontoConcedido / 100;

                decimal valorPago = Convert.ToUInt64(registro.Substring(253, 13));
                detalhe.ValorPago = valorPago / 100;

                decimal jurosMora = Convert.ToUInt64(registro.Substring(266, 13));
                detalhe.JurosMora = jurosMora / 100;

                int dataCredito = Utils.ToInt32(registro.Substring(328, 8));
                detalhe.DataCredito = Utils.ToDateTime(dataCredito.ToString("####-##-##"));

                detalhe.MotivosRejeicao = registro.Substring(318, 10);

                detalhe.NomeSacado = registro.Substring(19, 5);
                return detalhe;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao ler detalhe do arquivo de RETORNO / CNAB 400.", ex);
            }
        }
        */
        #endregion M�todos de Leitura do Arquivo de Retorno

        public int Mod10Sicredi(string seq)
        {
            /* Vari�veis
             * -------------
             * d - D�gito
             * s - Soma
             * p - Peso
             * b - Base
             * r - Resto
             */

            int d, s = 0, p = 2, b = 2, r;

            for (int i = seq.Length - 1; i >= 0; i--)
            {

                r = (Convert.ToInt32(seq.Substring(i, 1)) * p);
                if (r > 9)
                    r = SomaDezena(r);
                s = s + r;
                if (p < b)
                    p++;
                else
                    p--;
            }

            d = Multiplo10(s);
            return d;
        }

        public int SomaDezena(int dezena)
        {
            string d = dezena.ToString();
            int d1 = Convert.ToInt32(d.Substring(0, 1));
            int d2 = Convert.ToInt32(d.Substring(1));
            return d1 + d2;
        }

        public int digSicredi(string seq)
        {
            /* Vari�veis
             * -------------
             * d - D�gito
             * s - Soma
             * p - Peso
             * b - Base
             * r - Resto
             */

            int d, s = 0, p = 2, b = 9;

            for (int i = seq.Length - 1; i >= 0; i--)
            {
                s = s + (Convert.ToInt32(seq.Substring(i, 1)) * p);
                if (p < b)
                    p = p + 1;
                else
                    p = 2;
            }

            d = 11 - (s % 11);
            if (d > 9)
                d = 0;
            return d;
        }

        public string DigNossoNumeroSicredi(Boleto boleto)
        {
            string codigoCedente = boleto.Cedente.Codigo;           //c�digo do benefici�rio aaaappccccc
            string nossoNumero = boleto.NossoNumero;                //ano atual (yy), indicador de gera��o do nosso n�mero (b) e o n�mero seq�encial do benefici�rio (nnnnn);

            string seq = string.Concat(codigoCedente, nossoNumero); // = aaaappcccccyybnnnnn
            /* Vari�veis
             * -------------
             * d - D�gito
             * s - Soma
             * p - Peso
             * b - Base
             * r - Resto
             */

            int d, s = 0, p = 2, b = 9;
            //Atribui os pesos de {2..9}
            for (int i = seq.Length - 1; i >= 0; i--)
            {
                s = s + (Convert.ToInt32(seq.Substring(i, 1)) * p);
                if (p < b)
                    p = p + 1;
                else
                    p = 2;
            }
            d = 11 - (s % 11);//Calcula o M�dulo 11;
            if (d > 9)
                d = 0;
            return d.ToString();
        }


        /// <summary>
        /// Efetua as Valida��es dentro da classe Boleto, para garantir a gera��o da remessa
        /// </summary>
        public override bool ValidarRemessa(TipoArquivo tipoArquivo, string numeroConvenio, IBanco banco, Cedente cedente, Boletos boletos, int numeroArquivoRemessa, out string mensagem)
        {
            bool vRetorno = true;
            string vMsg = string.Empty;
            //            
            switch (tipoArquivo)
            {
                case TipoArquivo.CNAB240:
                    //vRetorno = ValidarRemessaCNAB240(numeroConvenio, banco, cedente, boletos, numeroArquivoRemessa, out vMsg);
                    break;
                case TipoArquivo.CNAB400:
                    vRetorno = ValidarRemessaCNAB400(numeroConvenio, banco, cedente, boletos, numeroArquivoRemessa, out vMsg);
                    break;
                case TipoArquivo.Outro:
                    throw new Exception("Tipo de arquivo inexistente.");
            }
            //
            mensagem = vMsg;
            return vRetorno;
        }


        #region CNAB 400 - sidneiklein
        public bool ValidarRemessaCNAB400(string numeroConvenio, IBanco banco, Cedente cedente, Boletos boletos, int numeroArquivoRemessa, out string mensagem)
        {
            bool vRetorno = true;
            string vMsg = string.Empty;
            //
            #region Pr� Valida��es
            if (banco == null)
            {
                vMsg += String.Concat("Remessa: O Banco � Obrigat�rio!", Environment.NewLine);
                vRetorno = false;
            }
            if (cedente == null)
            {
                vMsg += String.Concat("Remessa: O Cedente/Benefici�rio � Obrigat�rio!", Environment.NewLine);
                vRetorno = false;
            }
            if (boletos == null || boletos.Count.Equals(0))
            {
                vMsg += String.Concat("Remessa: Dever� existir ao menos 1 boleto para gera��o da remessa!", Environment.NewLine);
                vRetorno = false;
            }
            #endregion
            //
            foreach (Boleto boleto in boletos)
            {
                #region Valida��o de cada boleto
                if (boleto.Remessa == null)
                {
                    vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; Remessa: Informe as diretrizes de remessa!", Environment.NewLine);
                    vRetorno = false;
                }
                else
                {
                    #region Valida��es da Remessa que dever�o estar preenchidas quando SICREDI
                    //Comentado porque ainda est� fixado em 01
                    //if (String.IsNullOrEmpty(boleto.Remessa.CodigoOcorrencia))
                    //{
                    //    vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; Remessa: Informe o C�digo de Ocorr�ncia!", Environment.NewLine);
                    //    vRetorno = false;
                    //}
                    if (String.IsNullOrEmpty(boleto.NumeroDocumento))
                    {
                        vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; Remessa: Informe um N�mero de Documento!", Environment.NewLine);
                        vRetorno = false;
                    }
                    else if (String.IsNullOrEmpty(boleto.Remessa.TipoDocumento))
                    {
                        // Para o Sicredi, defini o Tipo de Documento sendo: 
                        //       A = 'A' - SICREDI com Registro
                        //      C1 = 'C' - SICREDI sem Registro Impress�o Completa pelo Sicredi
                        //      C2 = 'C' - SICREDI sem Registro Pedido de bloquetos pr�-impressos
                        // ** Isso porque s�o tratados 3 leiautes de escrita diferentes para o Detail da remessa;

                        vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; Remessa: Informe o Tipo Documento!", Environment.NewLine);
                        vRetorno = false;
                    }
                    else if (!boleto.Remessa.TipoDocumento.Equals("A") && !boleto.Remessa.TipoDocumento.Equals("C1") && !boleto.Remessa.TipoDocumento.Equals("C2"))
                    {
                        vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; Remessa: Tipo de Documento Inv�lido! Dever�o ser: A = SICREDI com Registro; C1 = SICREDI sem Registro Impress�o Completa pelo Sicredi;  C2 = SICREDI sem Registro Pedido de bloquetos pr�-impressos", Environment.NewLine);
                        vRetorno = false;
                    }
                    //else if (boleto.Remessa.TipoDocumento.Equals("06") && !String.IsNullOrEmpty(boleto.NossoNumero))
                    //{
                    //    //Para o "Remessa.TipoDocumento = "06", n�o poder� ter NossoNumero Gerado!
                    //    vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; N�o pode existir NossoNumero para o Tipo Documento '06 - cobran�a escritural'!", Environment.NewLine);
                    //    vRetorno = false;
                    //}
                    else if (!boleto.EspecieDocumento.Codigo.Equals("A") && //A - Duplicata Mercantil por Indica��o
                             !boleto.EspecieDocumento.Codigo.Equals("B") && //B - Duplicata Rural;
                             !boleto.EspecieDocumento.Codigo.Equals("C") && //C - Nota Promiss�ria;
                             !boleto.EspecieDocumento.Codigo.Equals("D") && //D - Nota Promiss�ria Rural;
                             !boleto.EspecieDocumento.Codigo.Equals("E") && //E - Nota de Seguros;
                             !boleto.EspecieDocumento.Codigo.Equals("F") && //G � Recibo;

                             !boleto.EspecieDocumento.Codigo.Equals("H") && //H - Letra de C�mbio;
                             !boleto.EspecieDocumento.Codigo.Equals("I") && //I - Nota de D�bito;
                             !boleto.EspecieDocumento.Codigo.Equals("J") && //J - Duplicata de Servi�o por Indica��o;
                             !boleto.EspecieDocumento.Codigo.Equals("O") && //O � Boleto Proposta
                             !boleto.EspecieDocumento.Codigo.Equals("K") //K � Outros.
                            )
                    {
                        vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; Remessa: Informe o C�digo da Esp�cieDocumento! S�o Aceitas:{A,B,C,D,E,F,H,I,J,O,K}", Environment.NewLine);
                        vRetorno = false;
                    }
                    else if (!boleto.Sacado.CPFCNPJ.Length.Equals(11) && !boleto.Sacado.CPFCNPJ.Length.Equals(14))
                    {
                        vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; Remessa: Cpf/Cnpj diferente de 11/14 caracteres!", Environment.NewLine);
                        vRetorno = false;
                    }
                    else if (!boleto.NossoNumero.Length.Equals(8))
                    {
                        //sidnei.klein: Segundo defini��o recebida pelo Sicredi-RS, o Nosso N�mero sempre ter� somente 8 caracteres sem o DV que est� no boleto.DigitoNossoNumero
                        vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; Remessa: O Nosso N�mero diferente de 8 caracteres!", Environment.NewLine);
                        vRetorno = false;
                    }
                    else if (!boleto.TipoImpressao.Equals("A") && !boleto.TipoImpressao.Equals("B"))
                    {
                        vMsg += String.Concat("Boleto: ", boleto.NumeroDocumento, "; Tipo de Impress�o deve conter A - Normal ou B - Carn�", Environment.NewLine);
                        vRetorno = false;
                    }
                    #endregion
                }
                #endregion
            }
            //
            mensagem = vMsg;
            return vRetorno;
        }
        public string GerarHeaderRemessaCNAB400(int numeroConvenio, Cedente cedente, int numeroArquivoRemessa)
        {
            try
            {
                TRegistroEDI reg = new TRegistroEDI();
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0001, 001, 0, "0", ' '));                             //001-001
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0002, 001, 0, "1", ' '));                             //002-002
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0003, 007, 0, "REMESSA", ' '));                       //003-009
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0010, 002, 0, "01", ' '));                            //010-011
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0012, 015, 0, "COBRANCA", ' '));                      //012-026
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0027, 005, 0, cedente.ContaBancaria.Conta, ' '));     //027-031
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0032, 014, 0, cedente.CPFCNPJ, ' '));                 //032-045
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0046, 031, 0, "", ' '));                              //046-076
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0077, 003, 0, "748", ' '));                           //077-079
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0080, 015, 0, "SICREDI", ' '));                       //080-094
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediDataAAAAMMDD_________, 0095, 008, 0, DateTime.Now, ' '));                    //095-102
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0103, 008, 0, "", ' '));                              //103-110
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0111, 007, 0, numeroArquivoRemessa.ToString(), '0')); //111-117
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0118, 273, 0, "", ' '));                              //118-390
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0391, 004, 0, "2.00", ' '));                          //391-394
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0395, 006, 0, "000001", ' '));                        //395-400
                //
                reg.CodificarLinha();
                //
                string vLinha = reg.LinhaRegistro;
                string _header = Utils.SubstituiCaracteresEspeciais(vLinha);
                //
                return _header;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar HEADER do arquivo de remessa do CNAB400.", ex);
            }
        }

        public string GerarDetalheRemessaCNAB400(Boleto boleto, int numeroRegistro, TipoArquivo tipoArquivo)
        {
            base.GerarDetalheRemessa(boleto, numeroRegistro, tipoArquivo);
            return GerarDetalheRemessaCNAB400_A(boleto, numeroRegistro, tipoArquivo);
        }
        public string GerarDetalheRemessaCNAB400_A(Boleto boleto, int numeroRegistro, TipoArquivo tipoArquivo)
        {
            try
            {
                TRegistroEDI reg = new TRegistroEDI();
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0001, 001, 0, "1", ' '));                                       //001-001
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0002, 001, 0, "A", ' '));                                       //002-002  'A' - SICREDI com Registro
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0003, 001, 0, "A", ' '));                                       //003-003  'A' - Simples
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0004, 001, 0, boleto.TipoImpressao, ' '));                                       //004-004  'A' � Normal
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0005, 012, 0, string.Empty, ' '));                              //005-016
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0017, 001, 0, "A", ' '));                                       //017-017  Tipo de moeda: 'A' - REAL
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0018, 001, 0, "A", ' '));                                       //018-018  Tipo de desconto: 'A' - VALOR
                #region C�digo de Juros
                string CodJuros = "A";
                decimal ValorOuPercJuros = 0;
                if (boleto.JurosMora > 0)
                {
                    CodJuros = "A";
                    ValorOuPercJuros = boleto.JurosMora;
                }
                else if (boleto.PercJurosMora > 0)
                {
                    CodJuros = "B";
                    ValorOuPercJuros = boleto.PercJurosMora;
                }
                #endregion
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0019, 001, 0, CodJuros, ' '));                                  //019-019  Tipo de juros: 'A' - VALOR / 'B' PERCENTUAL
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0020, 028, 0, string.Empty, ' '));                              //020-047
                #region Nosso N�mero + DV
                string NossoNumero = boleto.NossoNumero.Replace("/", "").Replace("-", ""); // AA/BXXXXX-D
                string vAuxNossoNumeroComDV = NossoNumero;
                if (string.IsNullOrEmpty(boleto.DigitoNossoNumero) || NossoNumero.Length < 9)
                {
                    boleto.DigitoNossoNumero = DigNossoNumeroSicredi(boleto);
                    vAuxNossoNumeroComDV = NossoNumero + boleto.DigitoNossoNumero;
                }
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0048, 009, 0, vAuxNossoNumeroComDV, '0'));                      //048-056
                #endregion
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0057, 006, 0, string.Empty, ' '));                              //057-062
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediDataAAAAMMDD_________, 0063, 008, 0, boleto.DataProcessamento, ' '));                  //063-070
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0071, 001, 0, string.Empty, ' '));                              //071-071
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0072, 001, 0, "N", ' '));                                       //072-072 'N' - N�o Postar e remeter para o benefici�rio
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0073, 001, 0, string.Empty, ' '));                              //073-073
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0074, 001, 0, "B", ' '));                                       //074-074 'B' � Impress�o � feita pelo Benefici�rio
                if (boleto.TipoImpressao.Equals("A"))
                {
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0075, 002, 0, 0, '0'));                                      //075-076
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0077, 002, 0, 0, '0'));                                      //077-078
                }
                else if (boleto.TipoImpressao.Equals("B"))
                {
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0075, 002, 0, boleto.NumeroParcela, '0'));                   //075-076
                    reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0077, 002, 0, boleto.TotalParcela, '0'));                    //077-078
                }
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0079, 004, 0, string.Empty, ' '));                              //079-082
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0083, 010, 2, boleto.ValorDescontoAntecipacao, '0'));           //083-092
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0093, 004, 2, boleto.PercMulta, '0'));                          //093-096
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0097, 012, 0, string.Empty, ' '));                              //097-108
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0109, 002, 0, ObterCodigoDaOcorrencia(boleto), ' '));           //109-110 01 - Cadastro de t�tulo;
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0111, 010, 0, boleto.NumeroDocumento, ' '));                    //111-120
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediDataDDMMAA___________, 0121, 006, 0, boleto.DataVencimento, ' '));                     //121-126
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0127, 013, 2, boleto.ValorBoleto, '0'));                        //127-139
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0140, 009, 0, string.Empty, ' '));                              //140-148
                #region Esp�cie de documento
                //Adota Duplicata Mercantil p/ Indica��o como padr�o.
                var especieDoc = boleto.EspecieDocumento ?? new EspecieDocumento_Sicredi("A");
                #endregion
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0149, 001, 0, especieDoc.Codigo, ' '));                         //149-149
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0150, 001, 0, boleto.Aceite, ' '));                             //150-150
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediDataDDMMAA___________, 0151, 006, 0, boleto.DataDocumento, ' '));                  //151-156
                #region Instru��es
                string vInstrucao1 = "00"; //1� instru��o (2, N) Caso Queira colocar um cod de uma instru��o. ver no Manual caso nao coloca 00
                string vInstrucao2 = "00"; //2� instru��o (2, N) Caso Queira colocar um cod de uma instru��o. ver no Manual caso nao coloca 00
                foreach (IInstrucao instrucao in boleto.Instrucoes)
                {
                    switch ((EnumInstrucoes_Sicredi)instrucao.Codigo)
                    {
                        case EnumInstrucoes_Sicredi.AlteracaoOutrosDados_CancelamentoProtestoAutomatico:
                            vInstrucao1 = "00";
                            vInstrucao2 = "00";
                            break;
                        case EnumInstrucoes_Sicredi.PedidoProtesto:
                            vInstrucao1 = "06"; //Indicar o c�digo �06� - (Protesto)
                            vInstrucao2 = Utils.FitStringLength(instrucao.QuantidadeDias.ToString(), 2, 2, '0', 0, true, true, true);
                            break;
                    }
                }
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0157, 002, 0, vInstrucao1, '0'));                               //157-158
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0159, 002, 0, vInstrucao2, '0'));                               //159-160
                #endregion               
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0161, 013, 2, ValorOuPercJuros, '0'));                          //161-173 Valor/% de juros por dia de atraso
                #region DataDesconto
                string vDataDesconto = "000000";
                if (!boleto.DataDesconto.Equals(DateTime.MinValue))
                    vDataDesconto = boleto.DataDesconto.ToString("ddMMyy");
                #endregion
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0174, 006, 0, vDataDesconto, '0'));                             //174-179
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0180, 013, 2, boleto.ValorDesconto, '0'));                      //180-192
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0193, 013, 0, 0, '0'));                                         //193-205
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0206, 013, 2, boleto.Abatimento, '0'));                         //206-218
                #region Regra Tipo de Inscri��o Sacado
                string vCpfCnpjSac = "0";
                if (boleto.Sacado.CPFCNPJ.Length.Equals(11)) vCpfCnpjSac = "1"; //Cpf � sempre 11;
                else if (boleto.Sacado.CPFCNPJ.Length.Equals(14)) vCpfCnpjSac = "2"; //Cnpj � sempre 14;
                #endregion
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0219, 001, 0, vCpfCnpjSac, '0'));                               //219-219
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0220, 001, 0, "0", '0'));                                       //220-220
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0221, 014, 0, boleto.Sacado.CPFCNPJ, '0'));                     //221-234
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0235, 040, 0, boleto.Sacado.Nome.ToUpper(), ' '));              //235-274
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0275, 040, 0, boleto.Sacado.Endereco.EndComNumero.ToUpper(), ' '));      //275-314
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0315, 005, 0, 0, '0'));                                         //315-319
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0320, 006, 0, 0, '0'));                                         //320-325
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0326, 001, 0, string.Empty, ' '));                              //326-326
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0327, 008, 0, boleto.Sacado.Endereco.CEP, '0'));                //327-334
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0335, 005, 1, 0, '0'));                                         //335-339
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0340, 014, 0, string.Empty, ' '));                              //340-353
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0354, 041, 0, string.Empty, ' '));                              //354-394
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, numeroRegistro, '0'));                            //395-400
                //
                reg.CodificarLinha();
                //
                string _detalhe = Utils.SubstituiCaracteresEspeciais(reg.LinhaRegistro);
                //
                return _detalhe;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar DETALHE do arquivo CNAB400.", ex);
            }
        }

        public string GerarTrailerRemessa400(int numeroRegistro, Cedente cedente)
        {
            try
            {
                TRegistroEDI reg = new TRegistroEDI();
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0001, 001, 0, "9", ' '));                         //001-001
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0002, 001, 0, "1", ' '));                         //002-002
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0003, 003, 0, "748", ' '));                       //003-006
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0006, 005, 0, cedente.ContaBancaria.Conta, ' ')); //006-010
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0011, 384, 0, string.Empty, ' '));                //011-394
                reg.CamposEDI.Add(new TCampoRegistroEDI(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, numeroRegistro, '0'));              //395-400
                //
                reg.CodificarLinha();
                //
                string vLinha = reg.LinhaRegistro;
                string _trailer = Utils.SubstituiCaracteresEspeciais(vLinha);
                //
                return _trailer;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro durante a gera��o do registro TRAILER do arquivo de REMESSA.", ex);
            }
        }

        private string LerMotivoRejeicao(string codigorejeicao)
        {
            var rejeicao = String.Empty;

            if (codigorejeicao.Length >= 2)
            {
                #region LISTA DE MOTIVOS
                List<String> ocorrencias = new List<string>();

                ocorrencias.Add("01-C�digo do banco inv�lido");
                ocorrencias.Add("02-C�digo do registro detalhe inv�lido");
                ocorrencias.Add("03-C�digo da ocorr�ncia inv�lido");
                ocorrencias.Add("04-C�digo de ocorr�ncia n�o permitida para a carteira");
                ocorrencias.Add("05-C�digo de ocorr�ncia n�o num�rico");
                ocorrencias.Add("07-Cooperativa/ag�ncia/conta/d�gito inv�lidos");
                ocorrencias.Add("08-Nosso n�mero inv�lido");
                ocorrencias.Add("09-Nosso n�mero duplicado");
                ocorrencias.Add("10-Carteira inv�lida");
                ocorrencias.Add("14-T�tulo protestado");
                ocorrencias.Add("15-Cooperativa/carteira/ag�ncia/conta/nosso n�mero inv�lidos");
                ocorrencias.Add("16-Data de vencimento inv�lida");
                ocorrencias.Add("17-Data de vencimento anterior � data de emiss�o");
                ocorrencias.Add("18-Vencimento fora do prazo de opera��o");
                ocorrencias.Add("20-Valor do t�tulo inv�lido");
                ocorrencias.Add("21-Esp�cie do t�tulo inv�lida");
                ocorrencias.Add("22-Esp�cie n�o permitida para a carteira");
                ocorrencias.Add("24-Data de emiss�o inv�lida");
                ocorrencias.Add("29-Valor do desconto maior/igual ao valor do t�tulo");
                ocorrencias.Add("31-Concess�o de desconto - existe desconto anterior");
                ocorrencias.Add("33-Valor do abatimento inv�lido");
                ocorrencias.Add("34-Valor do abatimento maior/igual ao valor do t�tulo");
                ocorrencias.Add("36-Concess�o de abatimento - existe abatimento anterior");
                ocorrencias.Add("38-Prazo para protesto inv�lido");
                ocorrencias.Add("39-Pedido para protesto n�o permitido para o t�tulo");
                ocorrencias.Add("40-T�tulo com ordem de protesto emitida");
                ocorrencias.Add("41-Pedido cancelamento/susta��o sem instru��o de protesto");
                ocorrencias.Add("44-Cooperativa de cr�dito/ag�ncia benefici�ria n�o prevista");
                ocorrencias.Add("45-Nome do pagador inv�lido");
                ocorrencias.Add("46-Tipo/n�mero de inscri��o do pagador inv�lidos");
                ocorrencias.Add("47-Endere�o do pagador n�o informado");
                ocorrencias.Add("48-CEP irregular");
                ocorrencias.Add("49-N�mero de Inscri��o do pagador/avalista inv�lido");
                ocorrencias.Add("50-Pagador/avalista n�o informado");
                ocorrencias.Add("60-Movimento para t�tulo n�o cadastrado");
                ocorrencias.Add("63-Entrada para t�tulo j� cadastrado");
                ocorrencias.Add("A -Aceito");
                ocorrencias.Add("A1-Pra�a do pagador n�o cadastrada.");
                ocorrencias.Add("A2-Tipo de cobran�a do t�tulo divergente com a pra�a do pagador.");
                ocorrencias.Add("A3-Cooperativa/ag�ncia deposit�ria divergente: atualiza o cadastro de pra�as da Coop./ag�ncia benefici�ria");
                ocorrencias.Add("A4-Benefici�rio n�o cadastrado ou possui CGC/CIC inv�lido");
                ocorrencias.Add("A5-Pagador n�o cadastrado");
                ocorrencias.Add("A6-Data da instru��o/ocorr�ncia inv�lida");
                ocorrencias.Add("A7-Ocorr�ncia n�o pode ser comandada");
                ocorrencias.Add("A8-Recebimento da liquida��o fora da rede Sicredi - via compensa��o eletr�nica");
                ocorrencias.Add("B4-Tipo de moeda inv�lido");
                ocorrencias.Add("B5-Tipo de desconto/juros inv�lido");
                ocorrencias.Add("B6-Mensagem padr�o n�o cadastrada");
                ocorrencias.Add("B7-Seu n�mero inv�lido");
                ocorrencias.Add("B8-Percentual de multa inv�lido");
                ocorrencias.Add("B9-Valor ou percentual de juros inv�lido");
                ocorrencias.Add("C1-Data limite para concess�o de desconto inv�lida");
                ocorrencias.Add("C2-Aceite do t�tulo inv�lido");
                ocorrencias.Add("C3-Campo alterado na instru��o �31 � altera��o de outros dados� inv�lido");
                ocorrencias.Add("C4-T�tulo ainda n�o foi confirmado pela centralizadora");
                ocorrencias.Add("C5-T�tulo rejeitado pela centralizadora");
                ocorrencias.Add("C6-T�tulo j� liquidado");
                ocorrencias.Add("C7-T�tulo j� baixado");
                ocorrencias.Add("C8-Existe mesma instru��o pendente de confirma��o para este t�tulo");
                ocorrencias.Add("C9-Instru��o pr�via de concess�o de abatimento n�o existe ou n�o confirmada");
                ocorrencias.Add("D -Desprezado");
                ocorrencias.Add("D1-T�tulo dentro do prazo de vencimento (em dia);");
                ocorrencias.Add("D2-Esp�cie de documento n�o permite protesto de t�tulo");
                ocorrencias.Add("D3-T�tulo possui instru��o de baixa pendente de confirma��o");
                ocorrencias.Add("D4-Quantidade de mensagens padr�o excede o limite permitido");
                ocorrencias.Add("D5-Quantidade inv�lida no pedido de boletos pr�-impressos da cobran�a sem registro");
                ocorrencias.Add("D6-Tipo de impress�o inv�lida para cobran�a sem registro");
                ocorrencias.Add("D7-Cidade ou Estado do pagador n�o informado");
                ocorrencias.Add("D8-Seq��ncia para composi��o do nosso n�mero do ano atual esgotada");
                ocorrencias.Add("D9-Registro mensagem para t�tulo n�o cadastrado");
                ocorrencias.Add("E2-Registro complementar ao cadastro do t�tulo da cobran�a com e sem registro n�o cadastrado");
                ocorrencias.Add("E3-Tipo de postagem inv�lido, diferente de S, N e branco");
                ocorrencias.Add("E4-Pedido de boletos pr�-impressos");
                ocorrencias.Add("E5-Confirma��o/rejei��o para pedidos de boletos n�o cadastrado");
                ocorrencias.Add("E6-Pagador/avalista n�o cadastrado");
                ocorrencias.Add("E7-Informa��o para atualiza��o do valor do t�tulo para protesto inv�lido");
                ocorrencias.Add("E8-Tipo de impress�o inv�lido, diferente de A, B e branco");
                ocorrencias.Add("E9-C�digo do pagador do t�tulo divergente com o c�digo da cooperativa de cr�dito");
                ocorrencias.Add("F1-Liquidado no sistema do cliente");
                ocorrencias.Add("F2-Baixado no sistema do cliente");
                ocorrencias.Add("F3-Instru��o inv�lida, este t�tulo est� caucionado/descontado");
                ocorrencias.Add("F4-Instru��o fixa com caracteres inv�lidos");
                ocorrencias.Add("F6-Nosso n�mero / n�mero da parcela fora de seq��ncia � total de parcelas inv�lido");
                ocorrencias.Add("F7-Falta de comprovante de presta��o de servi�o");
                ocorrencias.Add("F8-Nome do benefici�rio incompleto / incorreto.");
                ocorrencias.Add("F9-CNPJ / CPF incompat�vel com o nome do pagador / Sacador Avalista");
                ocorrencias.Add("G1-CNPJ / CPF do pagador Incompat�vel com a esp�cie");
                ocorrencias.Add("G2-T�tulo aceito: sem a assinatura do pagador");
                ocorrencias.Add("G3-T�tulo aceito: rasurado ou rasgado");
                ocorrencias.Add("G4-T�tulo aceito: falta t�tulo (cooperativa/ag. benefici�ria dever� envi�-lo);");
                ocorrencias.Add("G5-Pra�a de pagamento incompat�vel com o endere�o");
                ocorrencias.Add("G6-T�tulo aceito: sem endosso ou benefici�rio irregular");
                ocorrencias.Add("G7-T�tulo aceito: valor por extenso diferente do valor num�rico");
                ocorrencias.Add("G8-Saldo maior que o valor do t�tulo");
                ocorrencias.Add("G9-Tipo de endosso inv�lido");
                ocorrencias.Add("H1-Nome do pagador incompleto / Incorreto");
                ocorrencias.Add("H2-Susta��o judicial");
                ocorrencias.Add("H3-Pagador n�o encontrado");
                ocorrencias.Add("H4-Altera��o de carteira");
                ocorrencias.Add("H5-Recebimento de liquida��o fora da rede Sicredi � VLB Inferior � Via Compensa��o");
                ocorrencias.Add("H6-Recebimento de liquida��o fora da rede Sicredi � VLB Superior � Via Compensa��o");
                ocorrencias.Add("H7-Esp�cie de documento necessita benefici�rio ou avalista PJ");
                ocorrencias.Add("H8-Recebimento de liquida��o fora da rede Sicredi � Conting�ncia Via Compe");
                ocorrencias.Add("H9-Dados do t�tulo n�o conferem com disquete");
                ocorrencias.Add("I1-Pagador e Sacador Avalista s�o a mesma pessoa");
                ocorrencias.Add("I2-Aguardar um dia �til ap�s o vencimento para protestar");
                ocorrencias.Add("I3-Data do vencimento rasurada");
                ocorrencias.Add("I4-Vencimento � extenso n�o confere com n�mero");
                ocorrencias.Add("I5-Falta data de vencimento no t�tulo");
                ocorrencias.Add("I6-DM/DMI sem comprovante autenticado ou declara��o");
                ocorrencias.Add("I7-Comprovante ileg�vel para confer�ncia e microfilmagem");
                ocorrencias.Add("I8-Nome solicitado n�o confere com emitente ou pagador");
                ocorrencias.Add("I9-Confirmar se s�o 2 emitentes. Se sim, indicar os dados dos 2");
                ocorrencias.Add("J1-Endere�o do pagador igual ao do pagador ou do portador");
                ocorrencias.Add("J2-Endere�o do apresentante incompleto ou n�o informado");
                ocorrencias.Add("J3-Rua/n�mero inexistente no endere�o");
                ocorrencias.Add("J4-Falta endosso do favorecido para o apresentante");
                ocorrencias.Add("J5-Data da emiss�o rasurada");
                ocorrencias.Add("J6-Falta assinatura do pagador no t�tulo");
                ocorrencias.Add("J7-Nome do apresentante n�o informado/incompleto/incorreto");
                ocorrencias.Add("J8-Erro de preenchimento do titulo");
                ocorrencias.Add("J9-Titulo com direito de regresso vencido");
                ocorrencias.Add("K1-Titulo apresentado em duplicidade");
                ocorrencias.Add("K2-Titulo j� protestado");
                ocorrencias.Add("K3-Letra de cambio vencida � falta aceite do pagador");
                ocorrencias.Add("K4-Falta declara��o de saldo assinada no t�tulo");
                ocorrencias.Add("K5-Contrato de cambio � Falta conta gr�fica");
                ocorrencias.Add("K6-Aus�ncia do documento f�sico");
                ocorrencias.Add("K7-Pagador falecido");
                ocorrencias.Add("K8-Pagador apresentou quita��o do t�tulo");
                ocorrencias.Add("K9-T�tulo de outra jurisdi��o territorial");
                ocorrencias.Add("L1-T�tulo com emiss�o anterior a concordata do pagador");
                ocorrencias.Add("L2-Pagador consta na lista de fal�ncia");
                ocorrencias.Add("L3-Apresentante n�o aceita publica��o de edital");
                ocorrencias.Add("L4-Dados do Pagador em Branco ou inv�lido");
                ocorrencias.Add("L5-C�digo do Pagador na ag�ncia benefici�ria est� duplicado");
                ocorrencias.Add("M1-Reconhecimento da d�vida pelo pagador");
                ocorrencias.Add("M2-N�o reconhecimento da d�vida pelo pagador");
                ocorrencias.Add("M3-Inclus�o de desconto 2 e desconto 3 inv�lida");
                ocorrencias.Add("X0-Pago com cheque");
                ocorrencias.Add("X1-Regulariza��o centralizadora � Rede Sicredi");
                ocorrencias.Add("X2-Regulariza��o centralizadora � Compensa��o");
                ocorrencias.Add("X3-Regulariza��o centralizadora � Banco correspondente");
                ocorrencias.Add("X4-Regulariza��o centralizadora - VLB Inferior - via compensa��o");
                ocorrencias.Add("X5-Regulariza��o centralizadora - VLB Superior - via compensa��o");
                ocorrencias.Add("X6-Pago com cheque � bloqueado 24 horas");
                ocorrencias.Add("X7-Pago com cheque � bloqueado 48 horas");
                ocorrencias.Add("X8-Pago com cheque � bloqueado 72 horas");
                ocorrencias.Add("X9-Pago com cheque � bloqueado 96 horas");
                ocorrencias.Add("XA-Pago com cheque � bloqueado 120 horas");
                ocorrencias.Add("XB-Pago com cheque � bloqueado 144 horas");
                #endregion

                var ocorrencia = (from s in ocorrencias where s.Substring(0, 2) == codigorejeicao.Substring(0, 2) select s).FirstOrDefault();

                if (ocorrencia != null)
                    rejeicao = ocorrencia;
            }

            return rejeicao;
        }

        public override DetalheRetorno LerDetalheRetornoCNAB400(string registro)
        {
            try
            {
                TRegistroEDI_Sicredi_Retorno reg = new TRegistroEDI_Sicredi_Retorno();
                //
                reg.LinhaRegistro = registro;
                reg.DecodificarLinha();

                //Passa para o detalhe as propriedades de reg;
                DetalheRetorno detalhe = new DetalheRetorno(registro);
                //
                detalhe.IdentificacaoDoRegistro = Utils.ToInt32(reg.IdentificacaoRegDetalhe);
                //Filler1
                //TipoCobranca
                //CodigoPagadorAgenciaBeneficiario
                detalhe.NomeSacado = reg.CodigoPagadorJuntoAssociado;
                //BoletoDDA
                //Filler2
                #region NossoNumeroSicredi
                detalhe.NossoNumeroComDV = reg.NossoNumeroSicredi;
                detalhe.NossoNumero = reg.NossoNumeroSicredi.Substring(0, reg.NossoNumeroSicredi.Length - 1); //Nosso N�mero sem o DV!
                detalhe.DACNossoNumero = reg.NossoNumeroSicredi.Substring(reg.NossoNumeroSicredi.Length - 1); //DV do Nosso Numero
                #endregion
                //Filler3
                detalhe.CodigoOcorrencia = Utils.ToInt32(reg.Ocorrencia);
                int dataOcorrencia = Utils.ToInt32(reg.DataOcorrencia);
                detalhe.DataOcorrencia = Utils.ToDateTime(dataOcorrencia.ToString("##-##-##"));

                //Descri��o da ocorr�ncia
                detalhe.DescricaoOcorrencia = new CodigoMovimento(748, detalhe.CodigoOcorrencia).Descricao;

                detalhe.NumeroDocumento = reg.SeuNumero;
                //Filler4
                if (!String.IsNullOrEmpty(reg.DataVencimento))
                {
                    int dataVencimento = Utils.ToInt32(reg.DataVencimento);
                    detalhe.DataVencimento = Utils.ToDateTime(dataVencimento.ToString("##-##-##"));
                }
                decimal valorTitulo = Convert.ToInt64(reg.ValorTitulo);
                detalhe.ValorTitulo = valorTitulo / 100;
                //Filler5
                //Despesas de cobran�a para os C�digos de Ocorr�ncia (Valor Despesa)
                if (!String.IsNullOrEmpty(reg.DespesasCobranca))
                {
                    decimal valorDespesa = Convert.ToUInt64(reg.DespesasCobranca);
                    detalhe.ValorDespesa = valorDespesa / 100;
                }
                //Outras despesas Custas de Protesto (Valor Outras Despesas)
                if (!String.IsNullOrEmpty(reg.DespesasCustasProtesto))
                {
                    decimal valorOutrasDespesas = Convert.ToUInt64(reg.DespesasCustasProtesto);
                    detalhe.ValorOutrasDespesas = valorOutrasDespesas / 100;
                }
                //Filler6
                //Abatimento Concedido sobre o T�tulo (Valor Abatimento Concedido)
                decimal valorAbatimento = Convert.ToUInt64(reg.AbatimentoConcedido);
                detalhe.ValorAbatimento = valorAbatimento / 100;
                //Desconto Concedido (Valor Desconto Concedido)
                decimal valorDesconto = Convert.ToUInt64(reg.DescontoConcedido);
                detalhe.Descontos = valorDesconto / 100;
                //Valor Pago
                decimal valorPago = Convert.ToUInt64(reg.ValorEfetivamentePago);
                detalhe.ValorPago = valorPago / 100;
                //Juros Mora
                decimal jurosMora = Convert.ToUInt64(reg.JurosMora);
                detalhe.JurosMora = jurosMora / 100;
                //Multa
                decimal multa = Convert.ToUInt64(reg.Multa);
                detalhe.ValorMulta = multa / 100;
                //Filler7
                //SomenteOcorrencia19
                //Filler8
                detalhe.MotivoCodigoOcorrencia = reg.MotivoOcorrencia;
                int dataCredito = Utils.ToInt32(reg.DataPrevistaLancamentoContaCorrente);
                detalhe.DataCredito = Utils.ToDateTime(dataCredito.ToString("####-##-##"));
                //Filler9
                detalhe.NumeroSequencial = Utils.ToInt32(reg.NumeroSequencialRegistro);
                //
                #region NAO RETORNADOS PELO SICREDI
                //detalhe.Especie = reg.TipoDocumento; //Verificar Esp�cie de Documentos...
                detalhe.OutrosCreditos = 0;
                detalhe.OrigemPagamento = String.Empty;
                detalhe.MotivoCodigoOcorrencia = reg.MotivoOcorrencia;
                //
                detalhe.IOF = 0;
                //Motivos das Rejei��es para os C�digos de Ocorr�ncia
                detalhe.MotivosRejeicao = LerMotivoRejeicao(detalhe.MotivoCodigoOcorrencia);
                //N�mero do Cart�rio
                detalhe.NumeroCartorio = 0;
                //N�mero do Protocolo
                detalhe.NumeroProtocolo = string.Empty;

                detalhe.CodigoInscricao = 0;
                detalhe.NumeroInscricao = string.Empty;
                detalhe.Agencia = 0;
                detalhe.Conta = header.Conta;
                detalhe.DACConta = header.DACConta;

                detalhe.NumeroControle = string.Empty;
                detalhe.IdentificacaoTitulo = string.Empty;
                //Banco Cobrador
                detalhe.CodigoBanco = 0;
                //Ag�ncia Cobradora
                detalhe.AgenciaCobradora = 0;
                #endregion
                //
                return detalhe;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao ler detalhe do arquivo de RETORNO / CNAB 400.", ex);
            }
        }

        public override HeaderRetorno LerHeaderRetornoCNAB400(string registro)
        {
            try
            {
                header = new HeaderRetorno(registro);
                header.TipoRegistro = Utils.ToInt32(registro.Substring(000, 1));
                header.CodigoRetorno = Utils.ToInt32(registro.Substring(001, 1));
                header.LiteralRetorno = registro.Substring(002, 7);
                header.CodigoServico = Utils.ToInt32(registro.Substring(009, 2));
                header.LiteralServico = registro.Substring(011, 15);
                string _conta = registro.Substring(026, 5);
                header.Conta = Utils.ToInt32(_conta.Substring(0, _conta.Length - 1));
                header.DACConta = Utils.ToInt32(_conta.Substring(_conta.Length - 1));
                header.ComplementoRegistro2 = registro.Substring(031, 14);
                header.CodigoBanco = Utils.ToInt32(registro.Substring(076, 3));
                header.NomeBanco = registro.Substring(079, 15);
                header.DataGeracao = Utils.ToDateTime(Utils.ToInt32(registro.Substring(094, 8)).ToString("##-##-##"));
                header.NumeroSequencialArquivoRetorno = Utils.ToInt32(registro.Substring(110, 7));
                header.Versao = registro.Substring(390, 5);
                header.NumeroSequencial = Utils.ToInt32(registro.Substring(394, 6));



                return header;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao ler header do arquivo de RETORNO / CNAB 400.", ex);
            }
        }

        #endregion

        public override long ObterNossoNumeroSemConvenioOuDigitoVerificador(long convenio, string nossoNumero)
        {
            long num;
            if (nossoNumero.Length >= 8 && long.TryParse(nossoNumero.Substring(0, 8), out num))
            {
                return num;
            }
            throw new BoletoNetException("Nosso n�mero � inv�lido!");
        }

        public string GerarDetalheMultaRemessaCNAB400(Boleto boleto, int numeroRegistro)
        {
            throw new NotImplementedException();
        }
    }
}