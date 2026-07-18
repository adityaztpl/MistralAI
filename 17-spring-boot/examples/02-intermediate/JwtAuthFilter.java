package com.example.demo.security;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.stream.Collectors;
import org.springframework.http.HttpHeaders;
import org.springframework.security.authentication.AbstractAuthenticationToken;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.security.oauth2.jwt.JwtDecoder;
import org.springframework.security.oauth2.jwt.JwtException;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

@Component
public class JwtAuthFilter extends OncePerRequestFilter {

    private final JwtDecoder jwtDecoder;

    public JwtAuthFilter(JwtDecoder jwtDecoder) {
        this.jwtDecoder = jwtDecoder;
    }

    @Override
    protected void doFilterInternal(
        HttpServletRequest request,
        HttpServletResponse response,
        FilterChain filterChain
    ) throws ServletException, IOException {
        String tokenValue = resolveBearerToken(request);
        if (tokenValue == null) {
            filterChain.doFilter(request, response);
            return;
        }

        try {
            Jwt jwt = jwtDecoder.decode(tokenValue);
            validateRequiredClaims(jwt);
            JwtAuthentication authentication = new JwtAuthentication(jwt, extractAuthorities(jwt));
            SecurityContextHolder.getContext().setAuthentication(authentication);
            filterChain.doFilter(request, response);
        } catch (JwtException | IllegalArgumentException ex) {
            SecurityContextHolder.clearContext();
            response.setStatus(HttpServletResponse.SC_UNAUTHORIZED);
            response.setContentType("application/problem+json");
            response.getWriter().write("""
                {"type":"https://errors.example.com/invalid-token","title":"Invalid bearer token","status":401}
                """);
        }
    }

    private String resolveBearerToken(HttpServletRequest request) {
        String header = request.getHeader(HttpHeaders.AUTHORIZATION);
        if (header == null || !header.startsWith("Bearer ")) {
            return null;
        }
        String token = header.substring(7).trim();
        return token.isEmpty() ? null : token;
    }

    private void validateRequiredClaims(Jwt jwt) {
        if (jwt.getSubject() == null || jwt.getSubject().isBlank()) {
            throw new IllegalArgumentException("JWT subject is required");
        }
        Instant expiresAt = jwt.getExpiresAt();
        if (expiresAt == null || expiresAt.isBefore(Instant.now())) {
            throw new IllegalArgumentException("JWT is expired or missing expiration");
        }
        if (jwt.getIssuer() == null) {
            throw new IllegalArgumentException("JWT issuer is required");
        }
    }

    private Collection<GrantedAuthority> extractAuthorities(Jwt jwt) {
        List<String> authorities = new ArrayList<>();
        authorities.addAll(readSpaceSeparatedClaim(jwt, "scope").stream()
            .map(scope -> "SCOPE_" + scope)
            .toList());
        authorities.addAll(readCollectionClaim(jwt, "roles").stream()
            .map(role -> role.startsWith("ROLE_") ? role : "ROLE_" + role)
            .toList());
        authorities.addAll(readCollectionClaim(jwt, "groups").stream()
            .map(group -> group.startsWith("ROLE_") ? group : "ROLE_" + group)
            .toList());

        return authorities.stream()
            .filter(Objects::nonNull)
            .map(String::trim)
            .filter(value -> !value.isBlank())
            .distinct()
            .map(SimpleGrantedAuthority::new)
            .collect(Collectors.toUnmodifiableList());
    }

    private List<String> readSpaceSeparatedClaim(Jwt jwt, String claim) {
        Object value = jwt.getClaims().get(claim);
        if (value instanceof String text && !text.isBlank()) {
            return List.of(text.split(" "));
        }
        return List.of();
    }

    @SuppressWarnings("unchecked")
    private List<String> readCollectionClaim(Jwt jwt, String claim) {
        Object value = jwt.getClaims().get(claim);
        if (value instanceof Collection<?> collection) {
            return collection.stream().map(String::valueOf).toList();
        }
        if (value instanceof Map<?, ?> map && map.get("values") instanceof Collection<?> collection) {
            return collection.stream().map(String::valueOf).toList();
        }
        return List.of();
    }

    private static final class JwtAuthentication extends AbstractAuthenticationToken {
        private final Jwt jwt;

        private JwtAuthentication(Jwt jwt, Collection<? extends GrantedAuthority> authorities) {
            super(authorities);
            this.jwt = jwt;
            setAuthenticated(true);
        }

        @Override
        public Object getCredentials() {
            return jwt.getTokenValue();
        }

        @Override
        public Object getPrincipal() {
            return jwt.getSubject();
        }

        public Jwt getToken() {
            return jwt;
        }
    }
}
