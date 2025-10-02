using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApiRefeicoes.Services
{
    public class FaceApiService
    {
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _personGroupId;
        private readonly ILogger<FaceApiService> _logger;

        private const string RecognitionModel = "recognition_04";
        private const string DetectionModel = "detection_03";


        public FaceApiService(IConfiguration configuration, ILogger<FaceApiService> logger)
        {
            _apiKey = configuration["AzureFaceApi:ApiKey"];
            _endpoint = configuration["AzureFaceApi:Endpoint"];
            _personGroupId = configuration["AzureFaceApi:PersonGroupId"];
            _logger = logger;
        }

        private IFaceClient GetClient()
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(_apiKey)) { Endpoint = _endpoint };
        }

        public async Task EnsurePersonGroupExistsAsync()
        {
            var client = GetClient();
            try
            {
                await client.PersonGroup.GetAsync(_personGroupId);
                _logger.LogInformation("Grupo de Pessoas '{PersonGroupId}' já existe.", _personGroupId);
            }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Grupo de Pessoas '{PersonGroupId}' não encontrado. A criar um novo com o modelo de reconhecimento correto.", _personGroupId);

                using (var httpClient = new HttpClient())
                {
                    var uri = $"{_endpoint}face/v1.0/persongroups/{_personGroupId}";
                    httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

                    var body = new
                    {
                        name = "Grupo de Colaboradores do Refeitório",
                        recognitionModel = RecognitionModel
                    };

                    var jsonBody = JsonConvert.SerializeObject(body);
                    using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                    {
                        var response = await httpClient.PutAsync(uri, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogError("Falha ao criar o PersonGroup manualmente. Status: {StatusCode}, Resposta: {ErrorContent}", response.StatusCode, errorContent);
                            throw new HttpRequestException($"Falha ao criar o PersonGroup: {errorContent}");
                        }
                    }
                }
                _logger.LogInformation("Grupo de Pessoas '{PersonGroupId}' criado com sucesso utilizando o modelo {RecognitionModel}.", _personGroupId, RecognitionModel);
            }
        }

        public async Task<Guid?> DetectFace(Stream imageStream)
        {
            var client = GetClient();
            try
            {
                if (!imageStream.CanSeek)
                {
                    using var memoryStream = new MemoryStream();
                    await imageStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    imageStream = memoryStream;
                }
                else
                {
                    imageStream.Position = 0;
                }

                var detectedFaces = await client.Face.DetectWithStreamAsync(imageStream, recognitionModel: RecognitionModel, detectionModel: DetectionModel);

                if (detectedFaces.Any())
                {
                    var firstFace = detectedFaces.First();
                    _logger.LogInformation("Face detetada com sucesso. FaceId: {FaceId}", firstFace.FaceId);
                    return firstFace.FaceId;
                }
                _logger.LogWarning("Nenhuma face detetada na imagem.");
                return null;
            }
            catch (APIErrorException apiEx)
            {
                _logger.LogError(apiEx, "Erro na API do Azure (Detetar): {Message}", apiEx.Body?.Error?.Message);
                return null;
            }
        }

        // --- INÍCIO DA CORREÇÃO ---
        // Método reintroduzido para o ReconhecimentoController
        public async Task<string> DetectFaceAndGetId(Stream imageStream)
        {
            Guid? faceId = await DetectFace(imageStream);
            return faceId?.ToString();
        }

        // Método reintroduzido para o ReconhecimentoController
        public async Task<(bool isIdentical, double confidence)> VerifyFaces(Guid faceId1, Guid faceId2)
        {
            var client = GetClient();
            try
            {
                var result = await client.Face.VerifyFaceToFaceAsync(faceId1, faceId2);
                return (result.IsIdentical, result.Confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar faces.");
                return (false, 0);
            }
        }
        // --- FIM DA CORREÇÃO ---

        public async Task<Guid?> IdentifyFaceAsync(Guid faceId)
        {
            var client = GetClient();
            try
            {
                await EnsurePersonGroupExistsAsync();
                var identifyResults = await client.Face.IdentifyAsync(new List<Guid> { faceId }, _personGroupId);
                var result = identifyResults.FirstOrDefault()?.Candidates.FirstOrDefault();
                if (result != null && result.Confidence > 0.5)
                {
                    _logger.LogInformation("Pessoa identificada: {PersonId} com confiança {Confidence}", result.PersonId, result.Confidence);
                    return result.PersonId;
                }
                _logger.LogWarning("Nenhuma pessoa correspondente encontrada no grupo para o FaceId fornecido.");
                return null;
            }
            catch (APIErrorException apiEx)
            {
                _logger.LogError(apiEx, "Erro na API do Azure (Identificar): {Message}", apiEx.Body?.Error?.Message);
                return null;
            }
        }

        public async Task<Guid> CreatePersonAsync(string name)
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            var person = await client.PersonGroupPerson.CreateAsync(_personGroupId, name);
            _logger.LogInformation("Pessoa '{Name}' criada no Azure com PersonId: {PersonId}", name, person.PersonId);
            return person.PersonId;
        }

        public async Task<PersistedFace> AddFaceToPersonAsync(Guid personId, Stream imageStream)
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            imageStream.Position = 0;
            var persistedFace = await client.PersonGroupPerson.AddFaceFromStreamAsync(_personGroupId, personId, imageStream, detectionModel: DetectionModel);
            _logger.LogInformation("Face adicionada à pessoa {PersonId}. PersistedFaceId: {PersistedFaceId}", personId, persistedFace.PersistedFaceId);
            return persistedFace;
        }

        public async Task TrainPersonGroupAsync()
        {
            await EnsurePersonGroupExistsAsync();
            var client = GetClient();
            _logger.LogInformation("--- Iniciando o comando de treino para o grupo: {PersonGroupId} ---", _personGroupId);
            await client.PersonGroup.TrainAsync(_personGroupId);
        }

        public async Task<TrainingStatus> GetTrainingStatusAsync()
        {
            var client = GetClient();
            return await client.PersonGroup.GetTrainingStatusAsync(_personGroupId);
        }

        public async Task<PersonGroup> GetPersonGroupAsync()
        {
            var client = GetClient();
            _logger.LogInformation("A obter detalhes do PersonGroup '{PersonGroupId}' do Azure.", _personGroupId);
            return await client.PersonGroup.GetAsync(_personGroupId);
        }

        public async Task DeletePersonGroupAsync()
        {
            var client = GetClient();
            try
            {
                await client.PersonGroup.DeleteAsync(_personGroupId);
                _logger.LogInformation("Grupo de Pessoas '{PersonGroupId}' apagado com sucesso.", _personGroupId);
            }
            catch (APIErrorException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Tentativa de apagar o Grupo de Pessoas '{PersonGroupId}', mas ele não foi encontrado.", _personGroupId);
            }
        }
    }
}