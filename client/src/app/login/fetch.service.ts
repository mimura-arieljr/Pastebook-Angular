import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { CurrentUserService } from '../current-user.service';

@Injectable({
  providedIn: 'root'
})
export class FetchService {

  constructor(private http: HttpClient) { }

  postLogin(data: any) {
    const httpOptions = {
      headers: new HttpHeaders({
        'Accept': 'text/html'
      }),
      responseType: 'text' as 'json'
    };
    return this.http.post<string>("http://localhost:5000/login", data, httpOptions)   
  }

}
