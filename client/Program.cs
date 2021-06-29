using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ccgenerator;
using Grpc.Core;

//aqui criamos nosso gRPC client, onde colocamos noosas regras de negocio e lógica de programaçao executada pelo cliente.
namespace client
{
    class Program
    {
        private static async Task ListCCnumbers(CreditCardService.CreditCardServiceClient client, string email) //Função ativar o stream da lista de cartões de crédito.
        {

            Console.Clear(); //Limpar a tela
            Console.WriteLine("All credit cards registered with this email:\n");
            var response = client.ListCC(new ListCreditCardsRequest() // Chamando o método gRPC ListCC e inicializando o parametro ListCreditCrdsRequest. alimentando a lista obtida na variavel response
            {
                Email = email // Definindo quais campos queremos filtrar  e alimentando o nosso request com o email entrado pelo usuário
            });


            while (await response.ResponseStream.MoveNext()) //inicializando o processo de multiplarespostas via stream, essa função while só para quando não há mais linhas na lista
            {
                Console.WriteLine(response.ResponseStream.Current.List.CreditCardNumber.ToString()); //Mostrando na tela linha a linha da nossa lista obtida. é necessário usar Stream pois são varias respostas do servidor. se fosse apenas 1 consulta e uma resposta, nao teria essa necessidade.
            }
            
        }

        /*private static void ListOneCC(CreditCardService.CreditCardServiceClient client, string email)////Exemplo de busca por um valor apenas, e não uma lista. 

        {
            try
            {
                var response = client.ListOneCC(new ListOneCCRequest()
                {
                    Email = email
                });

                Console.WriteLine(response.List.ToString());
            }
            catch (RpcException e)
            {
                Console.WriteLine(e.Status.Detail);
            }
        }*/ 

        static async Task Main(string[] args)
        {
            Channel channel = new Channel("localhost", 50052, ChannelCredentials.Insecure); //Inicializando um channel com o serveidor, esse channel é o canal de conexão com o servidor, onde colocamos os parametro de entrada (porta, credenciais e local)

            await channel.ConnectAsync().ContinueWith((task) => //Abrindo canal com servidor.
                {
                    if (task.Status == TaskStatus.RanToCompletion) //Caso canal aberto com secesso mosta a mensagem abaixo.
                        Console.WriteLine("Welcome to the Credit Card Generator gRPC with MongoDB");
                });

            var client = new CreditCardService.CreditCardServiceClient(channel); //Instanciando o serviço gRPC (.proto)
            var creditCardRequest = new CreditCards(); //Instancia de CreditCards (.proto)
            var listCheckRequest = new ListCheckRequest(); //Instancia de ListCheckRequest (.proto)
            int menu = 0; //Variavel usada para receber as escolhas do usuário.
            string email; //Variavel usada para receber o email do cliente.
            var emailcheck = new ListCreditCardsResponse();
            //-----------------------------------------Regex - Formatação aceita de email-----------------//
            string pattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
+ @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
+ @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            bool isvalid;
            //---------------------------------------------------------------------------------------------//
            int Exit() //Método que pergunta ao usuário se quer finalizar a sessão ou voltar ao menu principal
            {
                menu = 0; //Garantindo que toda vez que o método for chamado, a opção do menu esteja zerada.
                do //Estrutura de repetição que garante que o usuário entre apenas com as opçoes do menu, e com tratamento de exceções caso entre com formato errado.
                {
                    try
                    {
                        Console.Clear();
                        Console.WriteLine("Would you like to exit(1) or to return to Main Menu?(2)");
                        menu = (Convert.ToInt32(Console.ReadLine()));
                    }
                    catch (FormatException) //Execeção caso o formato de entrada seja errado.
                    {
                        Console.WriteLine("Wrong format");
                        Console.ReadKey();
                    }
                } while (menu != 1 && menu != 2); //Garantindo que a entrada aceita seja apenas 1 ou 2.
                Console.Clear();
                return menu; //retornar a opção escolhida pelo usuário
            };
            int Menu() //Método do Menu Principal, mesmo princípio do método Exit()
            {
                menu = 0;
                do
                {
                    try
                    {
                        Console.Clear();
                        Console.WriteLine("Menu:\nGenerate(1)\nConsult(2)\nExit(3)");
                        menu = Convert.ToInt32(Console.ReadLine());
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("Wrong format");
                        Console.ReadKey();
                    }
                } while (menu != 1 && menu != 2 && menu != 3);
                return menu;
            }

            while (menu != 3) //Estrutura de repetição das opções do  Menu Principal
            {

                if (Menu() == 1) //Opção 1 (Criar Número de Cartão de Crédito)
                {
                    do//Estrutura de repetição que pergunta o email do cliente, até ele colocar um email no formato válido.
                    {
                        Console.Clear();
                        Console.WriteLine("Which email would like to attach to your new credit card?");
                        creditCardRequest.Email = Console.ReadLine(); //Armazenando o email do usuário dentro da variável gRPC email
                        isvalid = regex.IsMatch(creditCardRequest.Email); //Verificando se a entrada do usuário é o padrão de formato de email.
                    } while (creditCardRequest.Email.StartsWith("") & !isvalid); //Verificação da estrutura de repetição, garante que a entrada padrão de email e que nao seja espaços vazios ou caso o usuário apenas clique ENTRER.

                    var createCCResponse = client.CreateCC(creditCardRequest); //Chamando função que cria os numeros aleatórios do Cartão de crédito, e armazena na variavel answer
                    var listcheck = client.ListCheck(listCheckRequest); //Verifica se o número de cartão gerado já existe no banco de dados.

                    Console.Clear();
                    Console.WriteLine("Credit Card Number: " + createCCResponse.Cardresponse); //Mostra na tela o número de cartão de crédito gerado

                    Console.ReadKey();
                    Console.Clear();
                }
                else if (menu == 2) //Opção 2 (Consultar Cartão de crédito)
                {
                    do
                    {
                        Console.Clear();
                        Console.WriteLine("------------Consult-------------\nWhat is your email?");
                        email = Console.ReadLine(); //Lê email do usuário e armazena na variável email
                        isvalid = regex.IsMatch(email); //Verifica se o email inserido é formato válido

                    } while (email.StartsWith("") & !isvalid); //Verifica se o email é formato válido para dar continuidade a estrutura de repetição
                    await ListCCnumbers(client, email); //Função de stream que mostra a lista de cartões de crédito cadastrado no email inserido. 
                    Console.ReadKey();
                }
                else //Opção 3 (Exit)
                {
                    channel.ShutdownAsync(); //fecha o canal com o servidor
                    System.Environment.Exit(0); //Fecha o sistema
                }

                if (Exit() == 1)
                {
                    channel.ShutdownAsync().Wait();
                    System.Environment.Exit(0);
                }
            }
            channel.ShutdownAsync().Wait();
            Console.ReadKey();
        }
    }
}
