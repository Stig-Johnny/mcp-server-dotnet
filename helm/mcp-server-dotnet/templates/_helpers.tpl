{{/*
Expand the name of the chart.
*/}}
{{- define "mcp-server-dotnet.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "mcp-server-dotnet.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "mcp-server-dotnet.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "mcp-server-dotnet.labels" -}}
helm.sh/chart: {{ include "mcp-server-dotnet.chart" . }}
{{ include "mcp-server-dotnet.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "mcp-server-dotnet.selectorLabels" -}}
app.kubernetes.io/name: {{ include "mcp-server-dotnet.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "mcp-server-dotnet.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "mcp-server-dotnet.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
API Labels
*/}}
{{- define "mcp-server-dotnet.api.labels" -}}
{{ include "mcp-server-dotnet.labels" . }}
app.kubernetes.io/component: api
{{- end }}

{{/*
API Selector Labels
*/}}
{{- define "mcp-server-dotnet.api.selectorLabels" -}}
{{ include "mcp-server-dotnet.selectorLabels" . }}
app.kubernetes.io/component: api
{{- end }}

{{/*
BFF Labels
*/}}
{{- define "mcp-server-dotnet.bff.labels" -}}
{{ include "mcp-server-dotnet.labels" . }}
app.kubernetes.io/component: bff
{{- end }}

{{/*
BFF Selector Labels
*/}}
{{- define "mcp-server-dotnet.bff.selectorLabels" -}}
{{ include "mcp-server-dotnet.selectorLabels" . }}
app.kubernetes.io/component: bff
{{- end }}

{{/*
Host Labels
*/}}
{{- define "mcp-server-dotnet.host.labels" -}}
{{ include "mcp-server-dotnet.labels" . }}
app.kubernetes.io/component: host
{{- end }}

{{/*
Host Selector Labels
*/}}
{{- define "mcp-server-dotnet.host.selectorLabels" -}}
{{ include "mcp-server-dotnet.selectorLabels" . }}
app.kubernetes.io/component: host
{{- end }}

{{/*
Image helper for API
*/}}
{{- define "mcp-server-dotnet.api.image" -}}
{{- $repository := .Values.api.image.repository | default (printf "%s/%s/mcp-server-api" .Values.global.registry .Values.global.repository) }}
{{- $tag := .Values.api.image.tag | default .Values.global.tag | default .Chart.AppVersion }}
{{- printf "%s:%s" $repository $tag }}
{{- end }}

{{/*
Image helper for BFF
*/}}
{{- define "mcp-server-dotnet.bff.image" -}}
{{- $repository := .Values.bff.image.repository | default (printf "%s/%s/mcp-server-bff" .Values.global.registry .Values.global.repository) }}
{{- $tag := .Values.bff.image.tag | default .Values.global.tag | default .Chart.AppVersion }}
{{- printf "%s:%s" $repository $tag }}
{{- end }}

{{/*
Image helper for Host
*/}}
{{- define "mcp-server-dotnet.host.image" -}}
{{- $repository := .Values.host.image.repository | default (printf "%s/%s/mcp-server-host" .Values.global.registry .Values.global.repository) }}
{{- $tag := .Values.host.image.tag | default .Values.global.tag | default .Chart.AppVersion }}
{{- printf "%s:%s" $repository $tag }}
{{- end }}