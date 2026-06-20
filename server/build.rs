fn main() {
    let version = std::fs::read_to_string("../VERSION")
        .expect("VERSION file not found at project root")
        .lines()
        .next()
        .unwrap_or("0.0.0.0")
        .trim()
        .to_string();
    println!("cargo:rustc-env=EXTRACHAT_VERSION={}", version);
}
