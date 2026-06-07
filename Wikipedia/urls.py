"""
URL configuration for Wikipedia project.

The `urlpatterns` list routes URLs to views. For more information please see:
    https://docs.djangoproject.com/en/6.0/topics/http/urls/
Examples:
Function views
    1. Add an import:  from my_app import views
    2. Add a URL to urlpatterns:  path('', views.home, name='home')
Class-based views
    1. Add an import:  from other_app.views import Home
    2. Add a URL to urlpatterns:  path('', Home.as_view(), name='home')
Including another URLconf
    1. Import the include() function: from django.urls import include, path
    2. Add a URL to urlpatterns:  path('blog/', include('blog.urls'))
"""
from django.contrib import admin
from django.urls import path
from Wiki import views
from django.conf import settings
from django.conf.urls.static import static
urlpatterns = [
    path('admin/', admin.site.urls),
    path('', views.main_page),
    path('article/<int:id>/', views.article_page),
    path('article/new/', views.new_article_page ), 
    path('articles/all/', views.all_article_page),
    path('article/by-name', views.any_page),
    path("article/<int:id>/edit/", views.edit_article, name="edit_article"),
]+static(settings.MEDIA_URL, document_root=settings.MEDIA_ROOT)#
