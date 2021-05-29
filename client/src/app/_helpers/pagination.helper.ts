import { HttpClient, HttpParams } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { PaginationResult } from '../_models/pagination';

export function getPaginatedResult<T>(url: any, params: any, http: HttpClient) {
  const paginatedResult: PaginationResult<T> = new PaginationResult<T>();

  return http.get<T>(url, { observe: 'response', params }).pipe(
    map((response) => {
      paginatedResult.result = response.body as T;
      if (response.headers.get('Pagination') !== null) {
        const pagination = response.headers.get('Pagination') as string;
        paginatedResult.pagination = JSON.parse(pagination);
      }

      return paginatedResult;
    })
  );
}

export function getPaginationHeaders(pageNumber: number, pageSize: number) {
  let params = new HttpParams();
  params = params.append('pageNumber', pageNumber.toString());
  params = params.append('pageSize', pageSize.toString());
  return params;
}
