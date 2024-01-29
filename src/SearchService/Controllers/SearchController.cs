using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;

namespace SearchService;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    /*

    [HttpGet]   
    public async Task<ActionResult<List<Item>>> SearchItems(string term, int pageNumber = 1, int pageSize =4)
    {
        var q = DB.PagedSearch<Item>();
        q.Sort(x => x.Ascending(a => a.Make));

        if (!string.IsNullOrEmpty(term))
        {
            q.Match(Search.Full, term).SortByTextScore();
        }

        q.PageNumber(pageNumber);
        q.PageSize(pageSize);

        var result = await q.ExecuteAsync();

         return Ok(new
        {
            results = result.Results,
            pageCount = result.PageCount,
            totalCount = result.TotalCount
        });
    } 
    */

   [HttpGet]   
    public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParams searchParams)
    {
        var query = DB.PagedSearch<Item, Item>();

        if (!string.IsNullOrEmpty(searchParams.SearchTerm))
        {
            query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
        }

        query = searchParams.OrderBy switch
        {
            "make" => query.Sort(x => x.Ascending(a => a.Make)),               
            "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),  // item added most recently
            _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))  // _ : default  -> default Sorting : auction ending soonest
        };

        query = searchParams.FilterBy switch
        {
            "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
            "endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6)   // ending within 6 hours
                && x.AuctionEnd > DateTime.UtcNow),     // auction is still alive
            _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow)  // 
        }; 
 
        if (!string.IsNullOrEmpty(searchParams.Seller))
        {
            query.Match(x => x.Seller == searchParams.Seller);
        }

        if (!string.IsNullOrEmpty(searchParams.Winner))
        {
            query.Match(x => x.Winner == searchParams.Winner);
        }

        query.PageNumber(searchParams.PageNumber);
        query.PageSize(searchParams.PageSize);

        var result = await query.ExecuteAsync();

        return Ok(new
        {
            results = result.Results,
            pageCount = result.PageCount,
            totalCount = result.TotalCount
        });
    }
}