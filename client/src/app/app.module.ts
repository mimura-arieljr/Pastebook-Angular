import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { MainComponent } from './main/main.component';
import { ProfileComponent } from './profile/profile.component';
import { FriendsComponent } from './friends/friends.component';
import { PageNotFoundComponent } from './page-not-found/page-not-found.component';
import { LoginComponent } from './login/login.component';
import { NewsfeedComponent } from './newsfeed/newsfeed.component';
import { TimelineComponent } from './timeline/timeline.component';
import { HeaderComponent } from './header/header.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import {MatIconModule} from '@angular/material/icon';
import { LeftSidebarComponent } from './left-sidebar/left-sidebar.component';
import { HttpClientModule } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { NgxSpinnerModule } from 'ngx-spinner';

@NgModule({
  declarations: [
    AppComponent,
    MainComponent,
    ProfileComponent,
    FriendsComponent,
    PageNotFoundComponent,
    LoginComponent,
    NewsfeedComponent,
    TimelineComponent,
    HeaderComponent,
    LeftSidebarComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    MatIconModule,
    HttpClientModule,
    FormsModule,
    NgxSpinnerModule
  ],
  providers: [DatePipe],
  bootstrap: [AppComponent]
})
export class AppModule { }
