using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace pokemonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PokedexController : ControllerBase
    {
        private static Dictionary<int, Pokedex> _pokemonDictionary = new Dictionary<int, Pokedex>();

        private readonly HttpClient _httpClient;

        public PokedexController()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(300);
        }

        [HttpGet("Atualizar_Pokedex")]
        public async Task<IActionResult> GetAllPokemon()
        {
            var response = await _httpClient.GetAsync("pokemon?limit=151");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var pokemonResults = JsonConvert.DeserializeObject<PokemonResults>(json);

                var fetchTasks = pokemonResults.Results.Select(async pokemonResult =>
                {
                    var pokemonResponse = await _httpClient.GetAsync(pokemonResult.Url);
                    if (pokemonResponse.IsSuccessStatusCode)
                    {
                        var pokemonJson = await pokemonResponse.Content.ReadAsStringAsync();
                        var pokemon = JsonConvert.DeserializeObject<Pokedex>(pokemonJson);
                        return pokemon;
                    }
                    return null;
                });

                var fetchedPokemons = await Task.WhenAll(fetchTasks);
                foreach (var pokemon in fetchedPokemons.Where(p => p != null))
                {
                    _pokemonDictionary[pokemon.Id] = pokemon;
                }

                var pokemonListWithSize = _pokemonDictionary.Select(p => p.Value).ToList();

                return Ok(pokemonListWithSize);
            }
            else
            {
                return StatusCode((int)response.StatusCode, "Erro ao atualizar pokedex");
            }
        }

        [HttpGet("{id}/Buscar_Pokemon")]
        public IActionResult GetPokemonById(int id)
        {
            if (_pokemonDictionary.TryGetValue(id, out var pokemon))
            {
                return Ok(pokemon);
            }

            return NotFound("Pokemon não encontrado, verifique se o Pokémon está cadastrado");
        }

        [HttpPost("Adicionar_Pokemon")]
        public IActionResult AddPokemon([FromBody] Pokedex pokemon)
        {
            _pokemonDictionary[pokemon.Id] = pokemon;
            return Ok();
        }

        [HttpPut("{id}/Atualizar_Pokemon")]
        public IActionResult UpdatePokemon(int id, [FromBody] Pokedex updatedPokemon)
        {
            if (_pokemonDictionary.TryGetValue(id, out var pokemon))
            {
                pokemon.Name = updatedPokemon.Name;

                return Ok("Pokemon atualizado com sucesso");
            }

            return NotFound("Pokemon não encontrado, verifique se o Pokémon está cadastrado");
        }

        [HttpDelete("{id}/Remover_Pokemon")]
        public IActionResult DeletePokemon(int id)
        {
            if (_pokemonDictionary.TryGetValue(id, out var pokemon))
            {
                _pokemonDictionary.Remove(id);
                return Ok("Pokemon retirado da pokedex");
            }

            return NotFound("Pokemon não encontrado, verifique se o Pokémon está cadastrado");
        }

        private class PokemonResults
        {
            public List<PokemonResult> Results { get; set; }
        }

        private class PokemonResult
        {
            public string Url { get; set; }
        }
    }
}
