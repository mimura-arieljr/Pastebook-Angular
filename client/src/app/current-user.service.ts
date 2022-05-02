import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, OnInit } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

interface CurrentUserModel {
  FirstName: string;
  LastName: string;
  Username: string;
  ImageSrc: string;
}

@Injectable({
  providedIn: 'root'
})
export class CurrentUserService {
  isLoggedIn$ = new BehaviorSubject<boolean>(false);
  currentUser$ = new BehaviorSubject<CurrentUserModel>({
    FirstName: '',
    LastName: '',
    Username: '',
    ImageSrc: ''
  })

  constructor(private http: HttpClient) { }

  checkLocalStorage(): void {
    const JWT = localStorage.getItem('JSONWebToken')

    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'Accept': 'text/html',
          'AuthToken': JWT
        }),
        responseType: 'text' as 'json'
      };
      this.http.post<string>("http://localhost:5000/verifylocalstoragetoken", JWT, httpOptions)
        .subscribe((res) => {
          if (res === 'VALID') {
            this.isLoggedIn$.next(true);
            this.getCurrentUser();
          }
          else {
            localStorage.setItem('JSONWebToken', '')
          }
        })
    }
  }

  onLogout() {
    localStorage.setItem('JSONWebToken', '')
    this.isLoggedIn$.next(false)
  }

  getCurrentUser() {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        }),
      };

      this.http.post<CurrentUserModel>("http://localhost:5000/getcurrent", null, httpOptions)
        .subscribe((res) => {
          this.currentUser$.next(res)
        })
    }
  }

}
